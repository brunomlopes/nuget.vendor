# Nuget.Vendor
Manage your tools and servers for a project via nugets. Simply and Quickly.

# How?

Create a `vendors.txt` file containing nuget sources and dependencies:

```
source nuget https://api.nuget.org/v3/index.json

nuget RavenDB.Server 3.5.5-patch-35246 # use the package id as the folder name
nuget Redis-64 2.8.4 into Redis # drop into a specific folder name
```

Run `NugetVendor --vendors vendors.txt --folder ./local` which will download both nugets into folders `local/Redis` and `local/RavenDB.Server`.

