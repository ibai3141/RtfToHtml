# Local RtfPipe snapshot

Upstream: https://github.com/erdomke/RtfPipe
Commit: 9851ab97d693037073d567d74346184ae2f3d4b3
License: MIT, retained in LICENSE and copied beside the published executable.

Local changes:
- Model/Builder.cs: promote the first row's RowTextAlign onto its containing table before grouping header rows.
- Html/HtmlVisitor.cs: emit justification for nested tables even when the parent has the same token (CSS margins do not inherit).
- Html/CssString.cs: map table center/right justification to horizontal auto margins after the margin shorthand. Left/default tables retain their existing trleft indentation. Paragraph text alignment is not changed.
- RtfPipe.csproj: build the snapshot for net9.0 without upstream packaging/signing or obsolete framework targets.

RTF justification is represented at table level using the first row. Mixed row justification and floating-table positioning are not newly supported by this patch. Original upstream tests and documentation are retained for reference; the application's executable regression harness is tests/Regression.
