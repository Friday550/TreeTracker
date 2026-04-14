[README.md](https://github.com/user-attachments/files/26716698/README.md)
# 🌲 TreeTracker

A Blazor Server web application for tracking manufacturing trees across departments on the plant floor. TreeTracker provides a real-time view of which shop orders are on which trees, where they are in the production process, and surfaces errors from the automated processing pipeline.

---

## Tech Stack

- **Frontend/Backend:** Blazor Server (.NET 8)
- **Database:** SQL Server
- **Data Access:** Dapper
- **Styling:** Custom CSS + Bootstrap

---

## Project Structure

```
TreeTracker/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor       # Main layout with sidebar, nav, and error polling
│   │   ├── PrintLayout.razor      # Blank layout used for the traveler print page
│   │   └── NavMenu.razor          # Navigation sidebar
│   └── Pages/
│       ├── Index.razor            # Home page — tabbed tree grid by location
│       ├── Search.razor           # Search page — find a tree by shop order number
│       ├── Traveler.razor         # Print traveler page with QR code
│       └── Log.razor              # Error log page — history of procedure run errors
├── Models/
│   ├── TreeTrackerItem.cs         # Maps to dbo.TreeTracker
│   └── TreeTrackerLog.cs          # Maps to dbo.TreeTrackerLog
├── Services/
│   ├── TreeService.cs             # Data access for tree and part queries
│   └── LogService.cs              # Data access for error log queries
├── wwwroot/
│   ├── app.css                    # Global styles
│   ├── traveler.css               # Print-specific styles for the traveler page
│   └── js/
│       └── traveler.js            # QR code generation (QRCode.js)
├── Program.cs                     # App entry point and service registration
└── appsettings.json               # Connection string configuration
```

---

## Database Objects

### Tables

#### `dbo.TreeTracker`
The main table tracking which shop orders are on which trees.

| Column | Type | Description |
|---|---|---|
| ID | int | Auto-increment primary key |
| ProjectID | nvarchar(10) | Project identifier |
| ShopOrderNo | nvarchar(15) | Shop order number (unique) |
| WorkOrderNo | nvarchar(15) | Work order number |
| TagNo | nvarchar(50) | Tag number |
| PartID | nvarchar(50) | Part identifier / part code |
| CurrentTree | nvarchar(5) | The tree this shop order is currently on |
| TimeAdded | datetime | When the record was added |
| TreeLocation | nvarchar(50) | Department/location (e.g. Conductor Line) |
| PreviousTree | nvarchar(5) | The previous tree (if moved) |

#### `dbo.TreeTrackerLog`
Error log table populated by the stored procedure.

| Column | Type | Description |
|---|---|---|
| ID | int | Auto-increment primary key |
| RunID | uniqueidentifier | Groups all errors from a single procedure run |
| ShopOrderNo | nvarchar(15) | The shop order that failed |
| ErrorType | nvarchar(50) | Type of error (see Error Types below) |
| ErrorMessage | nvarchar(500) | Detailed error description |
| LoggedAt | datetime | When the error was logged |

#### `tblTempSONProcessing`
Temporary processing table populated by the external system before calling the stored procedure.

| Column | Type | Description |
|---|---|---|
| TempSON | int | Parent shop order number |
| tempSubPartID | int | Sub-part ID belonging to the parent |
| TempTreeID | nvarchar(5) | Tree ID to assign (e.g. T-01) |

### Stored Procedure

#### `usp_GetMasterDataFromTemp`
Processes all records in `tblTempSONProcessing` and inserts valid shop orders into `dbo.TreeTracker`.

**Flow:**
1. Loops through `tblTempSONProcessing` ordered by shop order number (lowest first)
2. For each shop order, runs 4 validation checks (see below)
3. On success — inserts into `dbo.TreeTracker`
4. On failure — logs to `dbo.TreeTrackerLog` with a unique `RunID`
5. Deletes the processed shop order from `tblTempSONProcessing` regardless of outcome
6. Continues until `tblTempSONProcessing` is empty

**Validation Checks:**

| Check | Error Type | Description |
|---|---|---|
| Shop order exists in `prod.Master` | `MasterNotFound` | The SON has no matching record in prod.Master |
| Shop order not already in TreeTracker | `DuplicateShopOrder` | The SON already exists in dbo.TreeTracker |
| Stack type determinable from PartID | `InvalidPartCode` | Cannot determine D or T stack type from the part code |
| Subpart count matches expected | `ValidationFailure` | Actual subpart count does not match BarsPerStack × StacksRequired |

**Part Code Rules:**
- Stack type `D` (Double) = 2 stacks required
- Stack type `T` (Triple) = 3 stacks required
- Bars per stack calculated from part code segments: `TP` = +3, `BE` = +1, `N` = +1, `E` (not `BE`) = +1

---

## Features

### Home Page — Tree Grid
- Displays all trees grouped by department location in tabs
- Locations: Conductor Line, Coating Start, Coating Finish, Plating Start, Plating Finish, Final Assembly
- Empty locations always shown with a friendly message
- Click any tree card to view all parts on that tree in a modal

### Search Page
- Search by shop order number
- Displays which tree the shop order is on and its current location
- Opens the full parts modal on a match
- Supports searching by pressing Enter or clicking the Search button

### Traveler Page
- Accessible from any tree modal via the Print Traveler button
- Opens in a new tab with a clean print layout (no sidebar or nav)
- Displays the tree name, a QR code encoding the tree name, and a parts table
- QR code generated using QRCode.js
- Print button triggers the browser print dialog — the button is hidden when printing

### Error Log Page
- Displays all errors logged by the stored procedure
- Errors grouped by `RunID` (one group per procedure run)
- Each error shows the shop order, error type (color-coded badge), message, and timestamp

### Real-Time Error Notifications
- `MainLayout` polls `dbo.TreeTrackerLog` every 10 seconds
- When new errors are detected since the last check, a popup notification appears
- Popup shows a summary of the latest run's errors with a link to the full log page

---

## Setup

### Prerequisites
- .NET 8 SDK
- SQL Server
- Visual Studio 2022 or later

### NuGet Packages
```
Dapper
Microsoft.Data.SqlClient
```

Install via Package Manager Console:
```bash
dotnet add package Dapper
dotnet add package Microsoft.Data.SqlClient
```

### Connection String
Update `appsettings.json` with your SQL Server details:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DATABASE;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Database Setup
Run the following in SQL Server to create the required objects:

```sql
-- Log table
CREATE TABLE dbo.TreeTrackerLog
(
    ID           INT IDENTITY(1,1) PRIMARY KEY,
    RunID        UNIQUEIDENTIFIER NOT NULL,
    ShopOrderNo  NVARCHAR(15) NULL,
    ErrorType    NVARCHAR(50) NOT NULL,
    ErrorMessage NVARCHAR(500) NOT NULL,
    LoggedAt     DATETIME NOT NULL DEFAULT GETDATE()
);
```

Then create the stored procedure `usp_GetMasterDataFromTemp` as defined in the SQL scripts.

### Running the App
```bash
dotnet run
```
Or press **F5** in Visual Studio.

---

## Adding New Locations

Locations are defined as a static list in `TreeService.cs`. To add or rename a location, update the list:

```csharp
public static readonly List<string> Locations = new()
{
    "Conductor Line",
    "Coating Start",
    "Coating Finish",
    "Plating Start",
    "Plating Finish",
    "Final Assembly"
};
```

Also update `TreeLocation` values in `dbo.TreeTracker` to match the new names.

---

## External System Integration

The stored procedure is triggered by an external system. That system is responsible for:
1. Populating `tblTempSONProcessing` with the shop orders and sub-parts to process
2. Calling `usp_GetMasterDataFromTemp`

The web app does not trigger the procedure — it only reads from `dbo.TreeTracker` and `dbo.TreeTrackerLog`.
