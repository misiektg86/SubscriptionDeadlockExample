# Steps to reproduce
1. Run the EventStore in single node or as a cluster
2. Run the example
3. Suspend EventStore and Resume it (by ProcessExplorer etc.) or simple restart EventStore
4. Connected handler hangs on EventStore access
5. Stop exapmle
6. Uncomment ThreadPool code
7. Run Example
8. Repeat step 3
9. It works