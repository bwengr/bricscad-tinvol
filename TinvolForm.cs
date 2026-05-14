using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Teigha.Runtime;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using Bricscad.Civil;
using BricscadApp = Bricscad.ApplicationServices;
using _AcEd = Bricscad.EditorInput;
using _AcDb = Teigha.DatabaseServices;
using _AcGe = Teigha.Geometry;
using Bricscad.Runtime;

namespace BricscadTinvol
{
    public class TinvolCommands
    {
        private static TinvolForm _dialog = null;

        [CommandMethod("TINVOLGRID", CommandFlags.Modal)]
        public static void ShowTinvolGridDialog()
        {
            if (_dialog == null)
            {
                _dialog = new TinvolForm();
            }
            _dialog.ShowDialog();
        }
    }

    public class TinvolForm : Form
    {
        private Label lblTextHeight, lblInterval;
        private TextBox txtTextHeight;
        private TextBox txtInterval;
        private Button btnGenerate;
        private Label lblStatus;

        public TinvolForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "TIN Volume Grid Labels";
            this.Size = new System.Drawing.Size(310, 210);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 15;
            int labelW = 120;
            int controlX = 135;
            int controlW = 130;
            int rowH = 28;

            lblTextHeight = new Label()
            {
                Text = "Text Height (ft):",
                Location = new System.Drawing.Point(15, y + 3),
                Size = new System.Drawing.Size(labelW, 20)
            };
            txtTextHeight = new TextBox()
            {
                Location = new System.Drawing.Point(controlX, y),
                Size = new System.Drawing.Size(controlW, 20),
                Text = "1"
            };

            y += rowH + 5;
            lblInterval = new Label()
            {
                Text = "Grid Interval (ft):",
                Location = new System.Drawing.Point(15, y + 3),
                Size = new System.Drawing.Size(labelW, 20)
            };
            txtInterval = new TextBox()
            {
                Location = new System.Drawing.Point(controlX, y),
                Size = new System.Drawing.Size(controlW, 20),
                Text = "3"
            };

            y += rowH + 10;
            btnGenerate = new Button()
            {
                Text = "Generate Grid Labels",
                Location = new System.Drawing.Point(15, y),
                Size = new System.Drawing.Size(270, 32)
            };
            btnGenerate.Click += BtnGenerate_Click;

            y += rowH + 8;
            lblStatus = new Label()
            {
                Text = "",
                Location = new System.Drawing.Point(15, y),
                Size = new System.Drawing.Size(270, 20),
                ForeColor = System.Drawing.Color.Blue
            };

            this.Controls.AddRange(new Control[] { lblTextHeight, txtTextHeight, lblInterval, txtInterval, btnGenerate, lblStatus });
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                double textHeight = double.Parse(txtTextHeight.Text);
                double interval = double.Parse(txtInterval.Text);

                lblStatus.Text = "Select TIN Volume surface in BricsCAD...";
                this.Refresh();

                GenerateVolumeGrid(textHeight, interval);

                lblStatus.Text = "Done! Check cut-grid and fill-grid layers.";
            }
            catch (System.Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private void GenerateVolumeGrid(double textHeight, double gridInterval)
        {
            _AcEd.Editor ed = BricscadApp.Application.DocumentManager.MdiActiveDocument.Editor;
            _AcDb.Database db = _AcDb.HostApplicationServices.WorkingDatabase;

            _AcEd.PromptEntityOptions peo = new _AcEd.PromptEntityOptions("\nSelect TIN or TIN Volume surface: ");
            peo.SetRejectMessage("Not a TIN surface!");
            peo.AddAllowedClass(typeof(TinSurface), true);
            peo.AddAllowedClass(typeof(TinVolumeSurface), true);
            _AcEd.PromptEntityResult per = ed.GetEntity(peo);

            if (per.Status != _AcEd.PromptStatus.OK)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                TinSurfaceStatic surfaceStatic = tr.GetObject(per.ObjectId, OpenMode.ForRead) as TinSurfaceStatic;
                if (surfaceStatic == null)
                {
                    ed.WriteMessage("\nSelected entity is not a TIN or TIN Volume surface.");
                    return;
                }

                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);

                const string cutLayer = "cut-grid";
                const string fillLayer = "fill-grid";

                ObjectId cutLayerId = ObjectId.Null;
                ObjectId fillLayerId = ObjectId.Null;

                if (!lt.Has(cutLayer))
                {
                    LayerTableRecord ltr = new LayerTableRecord();
                    ltr.Name = cutLayer;
                    ltr.Color = Teigha.Colors.Color.FromRgb(255, 0, 0);
                    lt.Add(ltr);
                    tr.GetObject(ltr.Id, OpenMode.ForWrite);
                    cutLayerId = ltr.Id;
                }
                else
                {
                    cutLayerId = lt[cutLayer];
                }

                if (!lt.Has(fillLayer))
                {
                    LayerTableRecord ltr = new LayerTableRecord();
                    ltr.Name = fillLayer;
                    ltr.Color = Teigha.Colors.Color.FromRgb(0, 180, 0);
                    lt.Add(ltr);
                    tr.GetObject(ltr.Id, OpenMode.ForWrite);
                    fillLayerId = ltr.Id;
                }
                else
                {
                    fillLayerId = lt[fillLayer];
                }

                Extents2d bounds = surfaceStatic.BoundingBox;
                double minX = bounds.MinPoint.X;
                double maxX = bounds.MaxPoint.X;
                double minY = bounds.MinPoint.Y;
                double maxY = bounds.MaxPoint.Y;

                double startX = Math.Ceiling(minX / gridInterval) * gridInterval;
                double startY = Math.Ceiling(minY / gridInterval) * gridInterval;

                double signHeight = textHeight * 0.6;
                double signOffset = textHeight * 0.5;

                int count = 0;
                int totalPoints = (int)(((maxX - startX) / gridInterval + 1) * ((maxY - startY) / gridInterval + 1));

                ed.WriteMessage("\nSampling {0} grid points (interval={1}ft, text height={2:F1}ft)...", totalPoints, gridInterval, textHeight);

                for (double x = startX; x <= maxX; x += gridInterval)
                {
                    for (double y = startY; y <= maxY; y += gridInterval)
                    {
                        _AcGe.Point3d samplePt = new _AcGe.Point3d(x, y, 0);
                        double z;
                        try
                        {
                            z = surfaceStatic.GetElevationAtPoint(samplePt);
                        }
                        catch
                        {
                            z = double.NaN;
                        }

                        if (double.IsNaN(z) || double.IsInfinity(z))
                            continue;

                        bool isFill = z < 0;
                        string sign = isFill ? "-" : "+";
                        string depthText = Math.Abs(z).ToString("F1");

                        ObjectId layerId = isFill ? fillLayerId : cutLayerId;

                        DBText mainText = new DBText();
                        mainText.SetDatabaseDefaults();
                        mainText.Position = new _AcGe.Point3d(x, y + signOffset, 0);
                        mainText.Height = textHeight;
                        mainText.TextString = depthText;
                        mainText.HorizontalMode = (_AcDb.TextHorizontalMode)1;
                        mainText.VerticalMode = (_AcDb.TextVerticalMode)1;
                        mainText.AlignmentPoint = mainText.Position;
                        mainText.LayerId = layerId;
                        ms.AppendEntity(mainText);

                        DBText signText = new DBText();
                        signText.SetDatabaseDefaults();
                        signText.Position = new _AcGe.Point3d(x, y, 0);
                        signText.Height = signHeight;
                        signText.TextString = sign;
                        signText.HorizontalMode = (_AcDb.TextHorizontalMode)1;
                        signText.VerticalMode = (_AcDb.TextVerticalMode)4;
                        signText.AlignmentPoint = signText.Position;
                        signText.LayerId = layerId;
                        ms.AppendEntity(signText);

                        count++;
                    }
                }

                ed.WriteMessage("\nCreated {0} labels ({1} cut, {2} fill).", count, count / 2, count / 2);

                tr.Commit();
            }
        }
    }
}