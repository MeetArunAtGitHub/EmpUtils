# Emp Utilities

Windows Forms troubleshooting tools for employee lookup, Qatch call analytics,
Qadence sessions, resources, and application logs. Targets .NET Framework 4.8
and C# 7.3; build with Visual Studio 2022 MSBuild.

## Build and validate

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe' EmpUtils.sln /p:Configuration=Release '/p:Platform=Any CPU' /m:1
& 'C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe' Tests\EmpUtils.Validation\EmpUtils.Validation.csproj /p:Configuration=Release /m:1
& .\Tests\EmpUtils.Validation\bin\Release\EmpUtils.Validation.exe
```

The standalone regression harness uses injected fake query runners and synthetic
data. It checks query parameters, match modes, cancellation, and layout at two
window sizes without reading connection settings or connecting to databases.
Set `EMPUTILS_SCREENSHOTS` to an output folder to save layout previews. UI checks
require a Windows desktop session. Test outputs and restored packages are ignored
by Git.

Run `EmpUtils\bin\Release\EmpUtils.exe` with the other files in that build folder
and the locally configured connection settings. Do not publish connection secrets
or generated previews containing real employee data. The executable embeds a
multi-resolution icon; after changing builds, unpin the old taskbar shortcut and
pin the current executable.

## Page behavior

- Home: employee lookup and tool navigation, result count and empty state.
- Qatch: call details and summaries, ANI exact/contains matching, optional
  exclusion of video-only records (`cd.CallId <> 99999`). Both date pickers expose
  time in Eastern time, and all four views preserve the selected start time.
- Qadence: UTC date/time bounds, exact case-insensitive domain/hostname filters,
  profit-center prefix, session details or distinct employee view.
- Resources: explicit Load (no per-keystroke queries), case-insensitive exact
  Domain ID by default, optional prefix matching and security-group prefix.
  The result is limited to 200 rows. `%` and `_` in prefix fields are literal.
- Logging: details or level-3 error-message counts, parameterized dates, domain,
  hostname, message contains, and comma-separated numeric application/subcomponent
  IDs. Optional custom SQL expressions remain an advanced trusted-user feature;
  they are not parsed or treated as ordinary text search.
- Qadence, Resources, and Logging run their bounded queries off the UI thread,
  display elapsed loading time, and offer Cancel. A canceled result is never
  applied to the grid. Closing an active page cancels its request first.

## Resource performance

The old Resource page queried on every edit and its Load button constructed an
incorrect domain condition. Explicit loading avoids overlapping repeated work;
the new exact mode avoids unnecessarily expanding a one-person lookup into a
prefix search. Parameterized filters fix quoting and resource-only lookup errors.
The grid resizes displayed columns once after loading rather than growing the form.

The live plan identified a more important bottleneck: filtering `lower(domainid)`
on the mapping table scanned approximately seven million rows. The app now applies
the equivalent case-insensitive filter to the joined employee table, then uses the
existing mapping-domain index for the equality join. A read-only representative
check on 2026-11-06 returned 93 rows in both plans: server execution changed from
3150 ms to 121 ms, and the optimized measured query round trip was 0.50 s. This is
one lookup, not a general performance guarantee. Initial connection setup measured
2.88–5.23 s separately; later pooled connections and different server/network loads
can differ.

Review the optional `Database/Resource-performance.sql` with the Automation DBA.
An employee lower-domain expression index could remove the remaining smaller
employee scan if justified. No indexes were changed for the measured improvement.
EmpUtils does not execute this script or change any database schema.

## Employee validation (configurable days plus today, Eastern)

After an employee lookup, select the employee row and click **Load details** in
the activity panel. This explicitly loads Qadence logins and Qatch daily call
counts in parallel; selecting rows alone never starts these extra queries.
The compact **Days** input defaults to **3** and accepts whole numbers from **1 to
90**, shared by both queries. Invalid values do not start queries. Changing Days
clears previous results; the input is disabled while loading.
The lower bound is midnight Eastern that many calendar dates before today, and the
upper bound is the current captured time (exclusive). For example, at November 6,
2026 14:00 EST with Days set to 3, the window is November 3 00:00 EST through November 6 14:00 EST.
Both queries use the same instants. The heading shows both boundaries in Eastern
time. This replaces the original rolling 72-hour window.

- Qadence uses the existing `Qadence` connection and `dbo.sessions` joined to
  `dbo.employee`. It reads session starts within the window, not sessions that
  began earlier and merely remained active. Query boundaries use UTC, but login
  and heartbeat timestamps are converted for display in Eastern time (EST/EDT,
  with DST handled automatically). Client IP and VDI IP are separate columns,
  decoded from the existing clip/vmip packed IPv4 values; hostnames remain separate.
  Only the newest 200 sessions are displayed, with a visible partial-results
  message if a 201st session exists.
- Qatch uses the existing `Qatch` connection and `dbo.call_details`. As confirmed
  for this dataset, CallDate is stored as Eastern wall-clock time, so its query
  boundaries use Eastern time via Windows `Eastern Standard Time` (including DST).
  Daily counts are grouped by Eastern date. Calls (all) includes video-only
  records; Video only uses CallId = 99999, and Calls excl. video uses the explicit
  CallId <> 99999 condition. The latter also excludes NULL CallIds, consistent with
  SQL comparison semantics, rather than being calculated as all minus video.
- The display includes the selected number of previous dates and today's partial
  day (four dates by default), except when the upper bound is exactly midnight. Dates with no calls
  are shown as zero only after a successful query.
  A database failure displays Unavailable, not a misleading zero count.
- Eastern timestamps without offsets cannot distinguish the duplicated hour at
  the autumn DST transition. An exact event-level distinction during that hour
  requires an offset/UTC timestamp in the source data.
- Each source displays its own loading/error/cancel state. Changing employee or
  rebinding employee results clears the panel and cancels old requests. Late
  responses from a former employee cannot replace the current results.
- These are read-only queries. No employee, session, or call records are changed.
