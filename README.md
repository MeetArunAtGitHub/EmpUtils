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
  exclusion of video-only records (`cd.CallId <> 99999`).
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

Actual database latency depends on indexes, join sizes, network, and server load.
No live timing improvement is claimed by the offline tests. Review the optional
`Database/Resource-performance.sql` with the Automation PostgreSQL DBA. It proposes
an active-mapping expression index matching the case-insensitive domain lookup and
an employee-domain join index. Check existing indexes and representative query
plans before adding anything. EmpUtils does not execute this script or change any
database schema.

## Employee validation (most recent 72 hours)

After an employee lookup, select the employee row and click **Load details** in
the activity panel. This explicitly loads Qadence logins and Qatch daily call
counts in parallel; selecting rows alone never starts these extra queries.
Both queries use the same captured start/end instants and an exclusive upper
bound. The page labels the employee and the window end in UTC.

- Qadence uses the existing `Qadence` connection and `dbo.sessions` joined to
  `dbo.employee`. It reads session starts within the window, not sessions that
  began earlier and merely remained active. Timestamps are displayed in UTC.
  Only the newest 200 sessions are displayed, with a visible partial-results
  message if a 201st session exists.
- Qatch uses the existing `Qatch` connection and `dbo.call_details`. As confirmed
  for this dataset, CallDate is stored as Eastern wall-clock time, so UTC window
  boundaries are converted using Windows `Eastern Standard Time` (including DST).
  Daily counts are grouped by Eastern date. Calls (all) includes video-only
  records; the separate Video only count uses CallId 99999.
- A rolling 72-hour interval can touch four calendar dates. Boundary dates are
  partial; dates with no calls are shown as zero only after a successful query.
  A database failure displays Unavailable, not a misleading zero count.
- Eastern timestamps without offsets cannot distinguish the duplicated hour at
  the autumn DST transition. An exact event-level distinction during that hour
  requires an offset/UTC timestamp in the source data.
- Each source displays its own loading/error/cancel state. Changing employee or
  rebinding employee results clears the panel and cancels old requests. Late
  responses from a former employee cannot replace the current results.
- These are read-only queries. No employee, session, or call records are changed.
