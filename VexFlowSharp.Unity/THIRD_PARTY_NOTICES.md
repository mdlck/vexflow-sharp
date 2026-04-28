# Third-Party Notices

This Unity package includes VexFlowSharp code plus font assets and generated
font data used for music notation rendering. The main VexFlowSharp source code
is licensed under the repository `LICENSE` file.

Keep this file with redistributed Unity packages.

## VexFlow

VexFlowSharp ports behavior from VexFlow.

- License: MIT
- Copyright:
  - Copyright (c) 2023-present VexFlow contributors
  - Copyright (c) 2010-2022 Mohit Muthanna Cheppudira

## Fonts

The package includes Bravura and Academico font binaries for UI Toolkit text
rendering. The generated `VexFlowSharp.Common` assembly may also include
generated font data for the VexFlow font packages.

Most VexFlow font packages are licensed under the SIL Open Font License 1.1.
Roboto Slab is licensed under Apache-2.0. Gonville font output is unrestricted
according to Simon Tatham's Gonville license notice; its source code uses the
MIT license.

See the repository-level `THIRD_PARTY_NOTICES.md` for the full font list and
license texts. If this Unity package is redistributed without the repository
root, include a copy of that repository-level notice file alongside this file.

## Build-Copied Plugin DLLs

Unity plugin DLLs copied into `Runtime/Plugins` by local builds are ignored by
git in this repository. If you distribute a package that contains those DLLs,
include the license notices for those copied assemblies as well.
