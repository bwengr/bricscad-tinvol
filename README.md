# BricscadTinvol

TIN Volume Grid Labels for BricsCAD Pro 2026.

Generates cut/fill depth labels on a grid across a TIN Volume Surface, placing text at each grid intersection showing depth values.

## Building from Source

### Requirements
- BricsCAD Pro 2026 (or compatible version)
- .NET 8 SDK
- Windows 11

### Build

```powershell
git clone https://github.com/bwengr/bricscad-tinvol.git
cd bricscad-tinvol
dotnet build -c Debug
```

Output: `bin\Debug\net8.0-windows\BricscadTinvol.dll`

## Installation

1. Build the DLL (see above)
2. In BricsCAD, type `NETLOAD`
3. Select `BricscadTinvol.dll`
4. The `TINVOLGRID` command is now available

## Usage

### Step 1: Create Your TIN Surfaces

**For existing surveyed surfaces (from contours):**
1. Draw your survey contours as 3D polylines
2. In BricsCAD, use `TINCREATESURFACE` command
3. Select all survey contours
4. Choose **Breaklines** when prompted
5. Select **No** for weeding factor
6. Select **No** for supplementing factors
7. Set midOrdinateDist to 0.1-0.5 for tight survey accuracy

**For proposed design surfaces:**
1. Draw your proposed contours as 3D polylines
2. Create TIN surface the same way as above
3. Use different layer or color to distinguish from survey

### Step 2: Create TIN Volume Surface

1. Select your **base surface** (survey) first
2. Select your **comparison surface** (proposed) second
3. BricsCAD creates a TIN Volume Surface

### Step 3: Generate Grid Labels

1. Type `TINVOLGRID` in BricsCAD
2. Enter **Text Height** in feet (e.g., 1 for full size, 0.1 for 1:10 scale)
3. Enter **Grid Interval** in feet (default 3ft for dense sampling)
4. Click **Generate Grid Labels**
5. Select your TIN Volume Surface when prompted
6. Labels are created on:
   - `cut-grid` layer (red) - cut values (comparison above base)
   - `fill-grid` layer (green) - fill values (base above comparison)

### Label Format

Each grid point gets two text entities:
- **Sign** (+ or -) at the grid intersection
- **Depth value** above the sign

Positive values (+) indicate fill (proposed surface is higher).
Negative values (-) indicate cut (proposed surface is lower).

## Workflow Tips

1. **Separate surfaces by layer** - Keep survey and proposed contours on different layers
2. **Use 3D polylines** - Breaklines honor elevation exactly; points interpolate
3. **Tight midOrdinateDist** - Use 0.1-0.5 for survey accuracy
4. **Verify surface accuracy** - Compare volume report against expected earthwork quantities

## Project Structure

```
bricscad-tinvol/
├── TinvolForm.cs          # Main plugin source (TINVOLGRID command + form)
├── BricscadTinvol.csproj # .NET project file
└── README.md              # This file
```

## License

MIT License - free to use, modify, and distribute.

## Credits

BricsCAD Pro .NET API documentation: https://developer.bricsys.com