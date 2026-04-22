# 🌲 TreeTracker
 
A Blazor Server web application for tracking manufacturing trees across departments on the plant floor. TreeTracker provides a real-time view of which shop orders are on which trees, where they are in the production process, surfaces errors from the automated processing pipeline, and allows manual management of trees and shop orders.
 
---
 
## Tech Stack
 
- **Frontend/Backend:** Blazor Server (.NET 8)
- **Database:** SQL Server
- **Data Access:** Dapper
- **Styling:** Custom CSS + Bootstrap
- **QR Code Generation:** QRCoder (server-side, no JavaScript dependency)
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
│       ├── Dashboard.razor        # Dashboard — live counts per location
│       ├── Traveler.razor         # Print traveler page with QR code
│       ├── ManualAdd.razor        # Manual shop order add page
│       ├── ManualLogPage.razor    # Manual action log page
│       └── Log.razor              # Error log page — history of procedure run errors
├── Models/
│   ├── TreeTrackerItem.cs         # Maps to dbo.TreeTracker
│   ├── TreeTrackerLog.cs          # Maps to dbo.TreeTrackerLog
│   ├── ManualLog.cs               # Maps to dbo.TreeTrackerManualLog
│   ├── LocationSummary.cs         # Dashboard location count model
│   ├── EngravingPart.cs           # Maps to ERPPBG.prod.tbl_EngravingLog
│   └── ServiceResult.cs           # Generic success/error result wrapper
├── Services/
│   ├── TreeService.cs             # Data access for tree and part queries
│   ├── LogService.cs              # Data access for error log queries
│   └── ManualLogService.cs        # Data access for manual action log queries
├── wwwroot/
│   ├── app.css                    # Global styles
│   ├── traveler.css               # Print-specific styles for the traveler page
│   └── images/
│       └── VertivLogo.png         # Company logo displayed in the app header
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
| PreviousTree | nvarchar(5) | The previous tree name (if moved) |
 
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
 
#### `dbo.TreeTrackerManualLog`
Audit log table for all manual actions performed through the web app.
 
| Column | Type | Description |
|---|---|---|
| ID | int | Auto-increment primary key |
| ActionType | nvarchar(10) | Either `MOVE` or `ADD` |
| ShopOrderNo | nvarchar(15) | Shop order number (for ADD actions) |
| TreeName | nvarchar(5) | The tree involved in the action |
| FromLocation | nvarchar(50) | Previous location (for MOVE actions) |
| ToLocation | nvarchar(50) | New location (for MOVE actions) |
| UserID | nvarchar(50) | ID of the user who performed the action |
| ActionAt | datetime | When the action was performed |
| Notes | nvarchar(500) | Auto-generated description of the action |
 
#### `tblTempSONProcessing`
Temporary processing table populated by the external system before calling the stored procedure.
 
| Column | Type | Description |
|---|---|---|
| TempSON | int | Parent shop order number |
| tempSubPartID | int | Sub-part ID belonging to the parent |
| TempTreeID | nvarchar(5) | Tree ID to assign (e.g. T-01) |
 
---
 
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
- **Move Tree** button inside the modal allows manually moving a tree to a new location
- **Print Traveler** button opens the traveler page in a new tab
### Dashboard
- Live snapshot of tree and shop order counts across all locations
- Summary bar showing total trees, total shop orders, and active location count
- Each location card shows tree count and shop order count with an Active/Empty badge
- Clicking a location card navigates to that tab on the home page
- Manual refresh button to pull latest counts
### Search Page
- Search by shop order number
- Displays which tree the shop order is on and its current location
- Opens the full parts modal on a match
- Supports searching by pressing Enter or clicking the Search button
### Traveler Page
- Accessible from any tree modal via the Print Traveler button
- Opens in a new tab with a clean print layout (no sidebar or nav)
- Displays the company logo, tree name, QR code encoding the tree name, and a parts table
- QR code generated server-side using QRCoder — no JavaScript dependency
- Print button triggers the browser print dialog — hidden when printing
### Manual Add Page
- Step-by-step workflow for manually adding a shop order to a tree
- Step 1: Enter shop order number — immediately validates against TreeTracker (duplicate check), prod.Master (existence check), and tbl_EngravingLog (parts check)
- Step 2: Verify all child parts from tbl_EngravingLog using checkboxes — all must be checked before proceeding. Only parts from the most recent batch (by CreatedAt date) are shown
- Step 3: Select a tree from a dropdown and enter a User ID (must be exactly 4 numeric digits)
- All manual adds are logged to `dbo.TreeTrackerManualLog`
### Move Tree
- Accessible from the tree detail modal on the home page
- Dropdown to select the new location (current location excluded)
- Requires a User ID before confirming
- Updates all parts on the tree to the new location
- Sets `PreviousTree` to the tree name before the move
- All manual moves are logged to `dbo.TreeTrackerManualLog`
### Manual Action Log Page
- Displays all manual moves and additions in a table
- Color coded badges: blue for ADD, yellow for MOVE
- Shows shop order, tree, from/to location, user ID, timestamp, and notes
### Error Log Page
- Displays all errors logged by the stored procedure
- Errors grouped by `RunID` (one group per procedure run)
- Each error shows the shop order, error type (color-coded badge), message, and timestamp
### Real-Time Error Notifications
- `MainLayout` polls `dbo.TreeTrackerLog` every 10 seconds
- When new errors are detected since the last check, a popup notification appears
- Popup shows a summary of the latest run's errors with a link to the full log page
---
 
## Error Handling
 
All service methods return a `ServiceResult` or `ServiceResult<T>` wrapper instead of throwing unhandled exceptions. Every database call is wrapped in a try/catch block that:
- Logs the full exception details via `ILogger`
- Returns a friendly error message to the UI
- Prevents the app from crashing on database errors
Pages display error banners when service calls fail, allowing users to see what went wrong without losing the rest of the page.
 
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
QRCoder
```
 
Install via Package Manager Console:
```bash
dotnet add "path/to/TreeTracker.csproj" package Dapper
dotnet add "path/to/TreeTracker.csproj" package Microsoft.Data.SqlClient
dotnet add "path/to/TreeTracker.csproj" package QRCoder
```
 
Or right-click the project in Visual Studio → **Manage NuGet Packages** and search for each package.
 
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
-- Error log table
CREATE TABLE dbo.TreeTrackerLog
(
    ID           INT IDENTITY(1,1) PRIMARY KEY,
    RunID        UNIQUEIDENTIFIER NOT NULL,
    ShopOrderNo  NVARCHAR(15) NULL,
    ErrorType    NVARCHAR(50) NOT NULL,
    ErrorMessage NVARCHAR(500) NOT NULL,
    LoggedAt     DATETIME NOT NULL DEFAULT GETDATE()
);
 
-- Manual action log table
CREATE TABLE dbo.TreeTrackerManualLog
(
    ID           INT IDENTITY(1,1) PRIMARY KEY,
    ActionType   NVARCHAR(10) NOT NULL,
    ShopOrderNo  NVARCHAR(15) NULL,
    TreeName     NVARCHAR(5) NULL,
    FromLocation NVARCHAR(50) NULL,
    ToLocation   NVARCHAR(50) NULL,
    UserID       NVARCHAR(50) NOT NULL,
    ActionAt     DATETIME NOT NULL DEFAULT GETDATE(),
    Notes        NVARCHAR(500) NULL
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
The web app does not trigger the procedure — it only reads from `dbo.TreeTracker` and `dbo.TreeTrackerLog`. Errors from each procedure run are surfaced to users via the real-time polling notification and the Error Log page.
