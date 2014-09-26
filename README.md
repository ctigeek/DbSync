DbSync
======
This will copy data from a MySql database to SqlServer database in near real-time.
 * It uses triggers to determine if a row has changed, and the service copies that row to the destination database.
 * Runs from the command line, or as a windows service.
 * Supports moving data from multiple databases to multiple databases.
 * Supports doing an initial bulk-copy of the data before syncing occurs.
 * Optionally writes to performance counters so you can track progress.
 * Writes data to destination db in a transaction to ensure consistency.
 

Limitations:
 * Currently there's a few things hard coded that only allow data to flow from a MySql db to SqlServer db.  It wouldn't take much coding to convert it to support other databases.
 * Doesn't support bi-directional data flow; one direction only. (Note: If you try to set up two instances of this to do both directions, it'll start an infinite loop of the same data moving back & forth.)
 * You have to create the triggers yourself. I'll include samples later, but they're not difficult. Ideally I'd like to include functionality to generate the triggers.
 * Doesn't self-recover is there's a problem copying a row of data.
 * Some data types still aren't compatible.
