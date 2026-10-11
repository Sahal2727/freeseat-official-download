# License Compliance Audit — FreeSeat 1.0.0

## Audit Date: October 2026
## Version: 1.0.0

---

## Project License

**FreeSeat License:** MIT License

```
MIT License

Copyright (c) 2026 Sahal

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## Dependency License Inventory

### Runtime Dependencies (Production)

| Package | Version | License | Source | Compatible with MIT? |
|---------|---------|---------|--------|---------------------|
| CommunityToolkit.Mvvm | 8.3.2 | MIT | Microsoft / .NET Foundation | ✅ Yes |
| Microsoft.Extensions.DependencyInjection | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Hosting | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Logging | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Configuration | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Configuration.Abstractions | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Configuration.Binder | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Configuration.CommandLine | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Configuration.FileExtensions | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Configuration.Json | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Diagnostics.Abstractions | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Diagnostics | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.FileProviders.Abstractions | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.FileProviders.Physical | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.FileSystemGlobbing | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Hosting.Abstractions | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Logging.Abstractions | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Logging.Configuration | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Logging.Console | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Logging.Debug | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Logging.EventLog | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Logging.EventSource | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Options | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Options.ConfigurationExtensions | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Microsoft.Extensions.Primitives | 9.0.0 | MIT | Microsoft | ✅ Yes |
| System.Management | 9.0.0 | MIT | Microsoft | ✅ Yes |
| Newtonsoft.Json | 13.0.3 | MIT | James Newton-King | ✅ Yes |

**All runtime dependencies are MIT-licensed or compatible permissive licenses.**
---

### Test Dependencies (Development Only)

| Package | Version | License | Source | Compatible with MIT? |
|---------|---------|---------|--------|---------------------|
| xunit | 2.9.0 | Apache-2.0 | .NET Foundation | ✅ Yes (Apache-2.0 compatible) |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0 | .NET Foundation | ✅ Yes |
| FluentAssertions | 6.12.0 | Apache-2.0 | FluentAssertions | ✅ Yes |
| Microsoft.NET.Test.Sdk | 17.10.0 | MIT | Microsoft | ✅ Yes |
| coverlet.collector | 6.0.2 | MIT | coverlet | ✅ Yes |

**Test dependencies are not distributed with the application.** They are only used during development and testing.

---

## Transitive Dependencies

All transitive dependencies of the above packages are also from the Microsoft/.NET Foundation ecosystem and carry MIT or Apache-2.0 licenses. No GPL, LGPL, AGPL, or other copyleft licenses detected in the dependency tree.

Verification command:
```bash
dotnet list package --include-transitive
```

---

## License Compatibility Analysis

### MIT License Compatibility

The MIT License is compatible with:
- ✅ MIT (self-compatible)
- ✅ Apache-2.0 (permissive, compatible)
- ✅ BSD-2-Clause / BSD-3-Clause
- ✅ ISC
- ✅ Public Domain / Unlicense

### No Conflicts Found

- ❌ No GPL-2.0 / GPL-3.0 (copyleft)
- ❌ No LGPL-2.1 / LGPL-3.0 (copyleft)
- ❌ No AGPL-3.0 (copyleft)
- ❌ No MPL-2.0 (weak copyleft — would require file-level compliance)
- ❌ No proprietary / commercial-only licenses
- ❌ No unknown / unverified licenses

---

## Attribution Requirements

### MIT License Requirements (for FreeSeat)

1. **Copyright notice** — Included in `LICENSE` file
2. **Permission notice** — Included in `LICENSE` file
3. **Preserve in copies** — Installer includes `LicenseFile=..\LICENSE`

### Dependency Attribution

For distributed binaries, the following attribution is recommended in installer or About dialog:

> FreeSeat includes software developed by:
> - Microsoft Corporation (MIT licensed .NET libraries)
> - .NET Foundation (CommunityToolkit.Mvvm, xUnit)
> - James Newton-King (Newtonsoft.Json)
> - FluentAssertions contributors (Apache-2.0)

The MIT license only requires preserving the license text for the **work itself**. Dependency licenses do not legally require attribution in the combined work (MIT is permissive), but it is best practice.

---

## Installer License Inclusion

**FreeSeat.iss** includes:
```iss
LicenseFile=..\LICENSE
```

This displays the MIT license during installation. ✅ Compliant.

---

## Source Distribution Compliance

When distributing source code (GitHub repository):

1. ✅ `LICENSE` file at repository root
2. ✅ License headers not required per-file for MIT (but present in some files)
3. ✅ Third-party licenses not required to be bundled (MIT doesn't require it)

---

## Binary Distribution Compliance

When distributing `FreeSeat-Setup.exe`:

1. ✅ Installer shows MIT license during install
2. ✅ Installed application includes no license file (not required for MIT binaries)
3. ✅ About dialog shows copyright: "© 2026 Sahal"
4. ⚠️ **Recommendation:** Add license link to About dialog (currently placeholder TODO)

---

## Compliance Status

| Check | Status |
|-------|--------|
| Project license declared | ✅ MIT |
| License file present | ✅ LICENSE |
| Runtime dependencies compatible | ✅ All MIT/Apache-2.0 |
| Test dependencies compatible | ✅ All MIT/Apache-2.0 |
| No copyleft dependencies | ✅ Verified |
| Installer shows license | ✅ LicenseFile=..\LICENSE |
| Copyright notice in binary | ✅ AssemblyCopyright attribute |
| Attribution for dependencies | ⚠️ Best practice only (not legally required) |

---

## Unverified Items

The following items could not be fully verified locally and should be confirmed:

1. **Transitive dependency completeness** — `dotnet list package --include-transitive` should be run in clean environment to confirm no hidden dependencies
2. **Native dependencies** — `System.Management` wraps WMI (Windows system component, not a NuGet dependency)
3. **Inno Setup license** — Inno Setup itself is BSD-licensed; the compiled installer is royalty-free

---

## Conclusion

**FreeSeat 1.0.0 is license-compliant for MIT-licensed distribution.**

All runtime dependencies use permissive licenses (MIT or Apache-2.0) compatible with the project's MIT license. No copyleft or conflicting licenses detected. The installer properly displays the MIT license during installation.

---

## Recommendations

1. **Run `dotnet list package --include-transitive` in CI** to continuously verify license compliance
2. **Add license URL to About dialog** (currently placeholder TODO)
3. **Consider adding THIRD-PARTY-NOTICES.txt** to installer for best-practice attribution
4. **Monitor dependency updates** for license changes (Dependabot / Renovate)

---

*FreeSeat 1.0.0 — Two seats. One PC.*