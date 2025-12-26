# Project Report Manager - Geometry Module (REVISED)

## 1. GENERAL INFORMATION
### 1.1 Project Overview
Desktop application for drilling operations management, specifically the Geometry module for wellbore and drill string configuration, designed for efficiency and minimal user clicks.

### 1.2 Architecture
- **Pattern**: MVVM (Model-View-ViewModel)
- **Platform**: Windows Desktop Application (WPF)
- **Technology Stack**: .NET 6+ / C# / WPF

### 1.3 Corporate Design System
**Color Palette**
- Primary Orange: `#EE7E32` (Primary buttons, accents)
- Gray: `#DADADA` (Secondary backgrounds, inactive elements)
- White: `#FFFFFF` (Main backgrounds)
- Black: `#000000` (Primary text)
- Dark Gray: `#70706F` (Secondary text, titles)

## 1.4 User Experience Principles
- **Minimize Clicks**: Maximum efficiency in data entry
- **Inline Editing**: Direct table editing without modal dialogs where possible
- **Smart Defaults**: Auto-population of logical values
- **Visual Feedback**: Immediate validation and calculation updates
- **Keyboard Support**: Tab navigation, Enter to save, shortcuts for common actions
- **Single-Page Workflow**: All related data visible without scrolling excessively

## 2. GLOBAL REQUIREMENTS (ALL MODULES)
### 2.1 Auto-incrementable Identifiers
- **Requirement**: All tables must include an auto-incrementable ID field
- **Format**: Numeric, consecutive (1, 2, 3, ...)
- **Visibility**: Visible to users for reference purposes
- **Behavior**: Auto-assigned on row creation, non-editable

### 2.2 Static Header
- **Fields**: WELL and REPORT NUMBER
- **Behavior**: Visible across all tabs/sub-modules, fixed position at top of interface, non-editable in Geometry module
- **Purpose**: Reserved for future integration with Well Data module
- **Visual Treatment**: Clearly distinguished (gray background or border)

### 2.3 Common User Interactions
#### Drag and Drop
- Row reordering via drag‑handle icon on left of each row
- Visual indicator during drag, auto‑revalidation after reorder
- Mobile alternative: up/down arrows

#### Inline Editing
- Double‑click or single‑click to edit cells
- Tab moves to next editable field, Enter saves and moves to next row, ESC cancels
- Auto‑save on field blur after 500 ms

#### Minimal Click Philosophy
- Inline “+” add buttons, hover‑delete “X” with undo toast
- Bulk operations via checkboxes
- Keyboard shortcuts: Ctrl+S (save), Ctrl+N (new row), etc.

## 3. NAVIGATION STRUCTURE
### 3.1 Main Modules
```
PROJECT REPORT MANAGER
├── Home (Future implementation)
└── Geometry (Active module)
    ├── Wellbore Geometry (Tab 1)
    ├── Drill String Geometry (Tab 2)
    ├── Survey (Tab 3)
    ├── Well Test (Tab 4)
    └── Summary (Tab 5)
```

### 3.2 Tab Navigation Requirements
- Horizontal tab interface below static header
- Active tab: orange underline/highlight (`#EE7E32`), bold text
- Inactive tabs: dimmed text
- Navigation: click, Ctrl+Tab / Ctrl+Shift+Tab, no data loss on switch
- Validation indicator: red badge on tab if errors exist

## 4. SUB‑MODULE: WELLBORE GEOMETRY
### 4.1 Purpose
Define the physical geometry of the wellbore including casings, liners, and open‑hole sections.

### 4.2 Data Table Structure
| Field | Unit | Type | Required | Description |
|---|---|---|---|---|
| ID | N/A | Auto‑increment | Yes | Unique section identifier (read‑only) |
| Section Type | N/A | Dropdown | Yes | Options: Casing, Liner, Open Hole |
| Name | N/A | Text | No | Section description |
| OD | in | Numeric | Conditional | Outer Diameter (disabled for Open Hole) |
| ID | in | Numeric | Yes | Inner Diameter |
| Top MD | ft | Numeric | Yes | Top Measured Depth |
| Bottom MD | ft | Numeric | Yes | Bottom Measured Depth |
| Volume | bbl | Calculated | N/A | Annular volume (auto‑calculated) |
| Actions | N/A | Icons | N/A | Drag handle, Delete icon |

### 4.3 Field Filling Order (Left to Right)
1. Section Type (determines field availability)
2. Name (optional)
3. OD (first diameter field)
4. ID (second diameter field)
5. Top MD
6. Bottom MD
7. Volume (read‑only)

### 4.4 Business Rules
- **BR‑WG‑001**: When Section Type = "Open Hole", disable OD (show N/A) and require only ID.
- **BR‑WG‑002**: Top MD of section N must **exactly** equal Bottom MD of section N‑1. Show modal error, block saving, highlight fields.
- **BR‑WG‑003**: Top MD must be **strictly less** than Bottom MD. Inline validation error.
- **BR‑WG‑004**: Drag‑and‑drop reordering must re‑validate continuity after drop.
- **BR‑WG‑005**: Auto‑incrementable numeric IDs, read‑only, unchanged after reorder.

### 4.5 Volume Calculations (WELLBORE SPECIFIC)
**Wellbore Annular Volume Formula** (SRS Section 4.5)
```
Volume (bbl) = (ID² / 1029.4) × Length
```
- `ID` = Inner Diameter (inches)
- `Length` = Bottom MD – Top MD (feet)
- `1029.4` = Conversion constant (bbl/ft from in²)
- Alternative: `π × (ID²/4) × Length / 5.615`
- Auto‑calculated, read‑only, displayed to 2 decimal places.
- Open Hole uses same formula (OD disabled).

### 4.6 Summary Display
- **Total Wellbore Depth**: Bottom MD of last section, formatted "X,XXX.XX ft", orange color, larger font.
- **Additional Metrics**: Total Wellbore Volume, Number of Sections, Deepest Casing.

### 4.7 UI Efficiency Features
- "+ Add Section" button (Ctrl+N) with smart defaults (Top MD = previous Bottom MD).
- Hover‑delete "X" with undo toast.
- Inline validation icons (✓/✗) and tooltips.

## 5. SUB‑MODULE: DRILL STRING GEOMETRY
### 5.1 Purpose
Configure drill‑string components from bottom to top with accurate volume calculations.

### 5.2 Data Table Structure
| Field | Unit | Type | Required | Description |
|---|---|---|---|---|
| ID | N/A | Auto‑increment | Yes | Unique component identifier (read‑only) |
| Component Type | N/A | Dropdown | Yes | See component types list |
| Name | N/A | Text | No | Component description |
| Length | ft | Numeric | Yes | Component length |
| ID | in | Numeric | Yes | Inner Diameter |
| OD | in | Numeric | Yes | Outer Diameter |
| Volume | bbl | Calculated | N/A | Displacement volume (auto‑calculated) |
| Configure | N/A | Button | N/A | Opens advanced configuration modal |
| Actions | N/A | Icons | N/A | Drag handle, Delete icon |

### 5.3 Component Types (Dropdown)
- Drill Pipe, HWDP, Casing, Liner, Setting Tool, DC, LWD, MWD, PWO, Motor, XO, JAR, Accelerator, Near Bit, Drill Bit, Bit

### 5.4 Field Filling Order (Left to Right)
1. Component Type
2. Name (optional)
3. Length (first measurement)
4. ID (inner diameter)
5. OD (outer diameter)
6. Volume (read‑only)
7. Configure button

### 5.5 Business Rules
- **BR‑DS‑001**: ID must be **strictly less** than OD. Inline validation error.
- **BR‑DS‑002**: Drag‑and‑drop reordering (bottom‑to‑top) – no continuity validation required.
- **BR‑DS‑003**: Auto‑incrementable numeric IDs, read‑only.
- **BR‑DS‑004**: *Force Drill String to Bottom* checkbox – calculates missing length and adjusts topmost component length, shows notification.

### 5.6 Volume Calculations (DRILL STRING)
**Displacement Volume Formula**
```
Displacement Volume (bbl) = (OD² - ID²) / 1029.4 × Length
```
- Auto‑calculated, displayed to 2 decimal places.

### 5.7 Calculated Fields & Displays
- Depth Tracking Summary Panel (total wellbore depth, total drill‑string length, missing feet, total volume) with color coding (green = at bottom, orange = off‑bottom, red = error).

### 5.8 Advanced Configuration Modal
- Trigger: gear icon or "Configure" button per row (Ctrl+C shortcut).
- Three modal types: Tubular Component Properties, Pressure Drop Configuration, Jets Configuration.
- Each modal contains required fields, validation, Save/Cancel buttons (orange/gray).

### 5.9 UI Efficiency Features
- "+ Add Component" (Ctrl+N) with smart defaults (pre‑select last type, focus first field).
- Inline editing, bulk operations via checkboxes, hover‑delete with undo.

## 6. SUB‑MODULE: SURVEY
### 6.1 Purpose
Record wellbore trajectory survey data points.

### 6.2 Data Table Structure
| Field | Unit | Type | Required | Description |
|---|---|---|---|---|
| ID | N/A | Auto‑increment | Yes | Unique point identifier (read‑only) |
| MD | ft | Numeric | Yes | Measured Depth (NOT "Section MD") |
| TVD | ft | Numeric | Yes | True Vertical Depth |
| Hole Angle | deg | Numeric | Yes | Inclination angle |
| Azimuth | deg | Numeric | Yes | Direction angle (0‑360) |
| Horizontal Displacement | ft | Numeric | No | Lateral displacement |
| Actions | N/A | Icons | N/A | Delete icon |

### 6.3 Business Rules
- **BR‑SV‑001**: MD ≥ TVD. Inline error if violated.
- **BR‑SV‑002**: Hole Angle ≤ 93°. Inline error if >93°.
- **BR‑SV‑003**: Azimuth ≤ 360°. Inline error if >360°.
- **BR‑SV‑004**: Auto‑incrementable point IDs.

### 6.4 Excel/CSV Import Functionality
- Import button (Ctrl+I) with file dialog, supports .xlsx and .csv (max 5 MB, 10 k rows).
- Column mapping UI if headers differ.
- Data preview, validation (apply all BR‑SV rules).
- Error report table with row numbers and messages.
- Options: fix file, import valid rows only, cancel.
- Post‑import toast, undo option.

### 6.5 UI Efficiency Features
- "+ Add Survey Point" (Ctrl+N) with auto‑populate MD pattern.
- Inline editing, tab navigation, bulk paste, sorting/filtering, search box.

## 7. SUB‑MODULE: WELL TEST
### 7.1 Purpose
Record well‑integrity and formation test data.

### 7.2 Data Table Structure
| Field | Type | Required | Description |
|---|---|---|---|
| ID | Auto‑increment | Yes | Unique test identifier (read‑only) |
| Section | Text | No | Free‑text description (open field) |
| Type | Dropdown | Yes | Component type under test |
| Test Value | numeric (ppb) | Yes | Pressure test value |
| Test Type | Dropdown | Yes | Test type (LOT, Fracture Gradient, etc.) |
| Actions | Icons | N/A | Delete icon |

### 7.3 Business Rules
- **BR‑WT‑001**: Section field must be free‑text (no dropdown).
- **BR‑WT‑002**: Auto‑incrementable test IDs.
- **BR‑WT‑003**: Test Value must be positive (0‑25 000 ppb).

## 8. SUB‑MODULE: SUMMARY
### 8.1 Purpose
Display consolidated calculations, detailed annular volumes, and visual representation.

### 8.2 Calculated Values Display
```
Total Wellbore Volume:   XXX.XX bbl
Total Drill String Volume: XXX.XX bbl
Total Annular Volume:    XXX.XX bbl
Total Circulation Volume: XXX.XX bbl
```
- Values auto‑updated in real‑time.

### 8.3 Annular Volume Details Table
Columns: ID, Section/Component Name, Wellbore ID (in), Drill String OD (in), Depth Range (ft), Annular Volume (bbl).
- Sortable, alternate row colors, highlight max volume, expandable rows, export to Excel/CSV.

### 8.4 Visual Scheme (Wellbore & Drill String Diagram)
- SVG/Canvas diagram showing concentric wellbore sections, drill‑string components, depth markers, diameter annotations, annular space shading.
- Interactive: hover tooltips, click to highlight, zoom/pan, reset view, export as PNG/JPG/PDF.

## 9. DATA VALIDATION AND ERROR HANDLING
### 9.1 Validation Levels
- **Client‑Side**: immediate feedback, numeric formats, range checks, logical rules (ID < OD, MD ≥ TVD, continuity).
- **Server‑Side**: business rule enforcement before persisting, referential integrity, cross‑module checks (drill‑string length vs wellbore depth).

### 9.2 Error Message Standards
- Inline field errors with icon and tooltip.
- Modal critical errors (continuity) with detailed description and fix buttons.
- Toast notifications for success/info.
- Summary panel for multiple errors with navigation links.

### 9.3 Validation Timing
- On field change (format, range).
- On field blur (complex logic).
- On row save (all rules).
- On tab switch (tab‑level indicator).
- Before final save (full system validation).

## 10. DATA PERSISTENCE AND STATE MANAGEMENT
### 10.1 Auto‑Save Requirements
- Trigger: field blur, 500 ms debounce.
- Indicator: "Saving..." spinner, then "✓ Saved".
- Manual Save button (Ctrl+S) also available.

### 10.2 Session Management
- Preserve data on tab switch, warn on application close if unsaved changes.
- Session timeout warning (if applicable for connected services), auto-save before close.
- Draft recovery via Local AppData / User Settings.

### 10.3 Data Export/Import
- Export per tab: Excel, CSV, PDF, JSON.
- Import currently only for Survey (Excel/CSV).

## 11. USER INTERFACE REQUIREMENTS
### 11.1 Minimal Click Philosophy
- Inline editing, smart defaults, keyboard‑first design, single‑click actions, contextual menus.
- Detailed UI element specs (buttons, inputs, tables).

### 11.2 Responsive Design Breakpoints
- Desktop (1920 px, 1440 px, 1024 px) – full features.
- Tablet (768 px) – stacked sections, touch‑optimized.
- Mobile (480 px, 320 px) – view‑only mode, "View on desktop to edit" notice.

### 11.3 Accessibility (WCAG 2.1 AA)
- Keyboard navigation, focus indicators, screen‑reader support, color contrast, scalable fonts, ARIA labels, live regions.

### 11.4 Internationalization (i18n)
- Primary: Spanish (Latin America)
- Secondary: English (US)
- Future: Portuguese (Brazil)
- Language switcher, resource files, locale‑specific formats, future unit conversion toggle.

## 12. NON‑FUNCTIONAL REQUIREMENTS
### 12.1 Performance
- View load < 2 s, tab switch < 500 ms, calculations < 500 ms, auto‑save < 1 s, export < 5 s, import < 10 s (1000 rows).
- Support ≥ 100 concurrent users, 10 000 survey points per well.
- Client‑side calculations, lazy loading, caching.

---
*Document generated as per user‑provided SRS specification.*
