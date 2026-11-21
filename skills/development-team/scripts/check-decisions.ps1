#Requires -Version 5.1
<#
.SYNOPSIS
    The development team's tool for its team folder: checks it, builds the overview
    (decisions.md), creates new items with the next free number, locks and unlocks files, and
    migrates the old single-file layout.

.DESCRIPTION
    Part of the development-team skill. Every task, question, decision and requirement is its
    own small file:
        tasks/T-001.md   questions/Q-001.md   decisions/D-001.md   requirements/REQ-001.md
    Each starts with a header between two '---' lines ("key: value" per line), followed by
    Markdown. Before an agent changes a file, it locks it (-Lock); the lock is a file in locks/.
    Each file has one writer at a time, so agents working in parallel do not overwrite each
    other. decisions.md is generated from the item files with -Build; nobody edits it by hand.

    Without switches the script only reads and reports. Errors give exit code 1; a lock held by
    someone else gives exit code 2.

.PARAMETER TeamFolder
    The team folder. Default: the folder this script is in (the script lives in the team
    folder), so it works from any current folder.

.PARAMETER Build
    Also write the overview to decisions.md (written to a temporary file, then swapped in).
    Without errors and lost items, it also saves an automatic checked copy to
    archive/<yyyy-MM-dd-HHmm>-auto-ok/ when the newest checked copy is 10 minutes old or more; the
    newest 48 automatic copies (8 hours) are kept.

.PARAMETER Overview
    Also print the team status and "How to proceed".

.PARAMETER SaveOk
    When there are no errors, save a copy of all item and document files (with the documents'
    folders, such as architecture/) to archive/<yyyy-MM-dd-HHmm>-ok/.

.PARAMETER Baseline
    A checked copy (folder) to compare with. Default: the newest archive/*-ok/ folder.

.PARAMETER New
    Create a new item with the next free number: task, question, decision or requirement.
    Use with -Title, and -Owner (task, decision), -From and -To (question), -WaitsFor (task),
    -Text (details, written below the title). A title of a task, decision or requirement has
    at most 80 characters. One task per item and role: when the role already has a task for the
    item (-For), the script refuses and names it; add the work to it with -Reopen.

.PARAMETER Reopen
    Give a task a new round of work: -Reopen T-033 -Text "<what to do now>". Adds the round
    under "## Task", sets a Done task back to Open and clears its merged: (the round goes into a
    new pull request). Then checks and rebuilds the overview.

.PARAMETER Lock
    Lock files before changing them: item ids (T-006, Q-024, D-012, REQ-011) or document names
    (architecture.md), several separated by commas: all of them or none. Use with -Owner. Exit
    code 2 when someone else holds one of them.

.PARAMETER Unlock
    Release locks after the change: names separated by commas, or "all" for every lock of
    -Owner. -Force releases someone else's lock. Add -Build to check and rebuild in the same
    call.

.PARAMETER LockMinutes
    After how many minutes a lock is stale and may be taken over. Default: 30.

.PARAMETER SilenceMinutes
    After how many minutes without a change an In progress task shows no sign of life. Default: 5.

.PARAMETER Migrate
    Convert the old single decisions.md (with a Task board table) into item files, once.

.PARAMETER Restore
    Restore lost or damaged files: "missing" (everything the check finds lost or empty), or
    names separated by commas (T-026, architecture.md, architecture/database.md). A file comes
    back from the newest checked copy that has it; an item no copy has is recreated under the
    same id from ids.log.
    A good file is never overwritten. Then checks and rebuilds the overview.

.PARAMETER UpdateFromSkill
    The skill folder (full path) to update the team from, in one step (skill update, step 1):
    copies RULES.md, HELP.md, this script, and the solution-type and prompt files the team has,
    and replaces the text of every agent definition of the team's tool with the skill's, keeping
    the definition's own header (name, description, model, tools). A mandatory role that is new
    in the skill (the code reviewer) gets a definition with the header of a role of the same kind
    and a row in the Team table of project.md; missing review templates are copied. Writes only
    files that differ. Then checks and rebuilds the overview.

.PARAMETER Stop
    Stop the team: -Stop finish (team stop: no new work, the running tasks are finished, the
    work is committed, reviewed and merged after the user's approval) or -Stop now (team
    emergency stop: every agent stops at its next step; no commit, push or merge). Writes
    stop.md in the team folder, with -Text as the reason. While -Stop now holds, -New, -Reopen
    and -Lock are refused. -Resume ends the stop.

.PARAMETER Resume
    End a stop: removes stop.md (team continue does this). Then checks and rebuilds the overview.

.PARAMETER Answer
    Answer a question: -Answer Q-039 -Text "<the answer>" [-Owner <who answers>]. Writes the
    answer under "## Answer", closes the question, and sets every task that now waits only for
    finished work back to Open. For a question to the user, the orchestrator records the
    user's words. Then checks and rebuilds the overview.

.PARAMETER SetStatus
    Change statuses in one call: -SetStatus "D-021=Superseded by D-026, D-026=Accepted,
    T-012=Open". Only the status words of RULES.md. Then checks and rebuilds the overview.

.PARAMETER Unblock
    Set every Blocked task whose waits are all finished back to Open, and clear its waits-for.
    Then checks and rebuilds the overview.

.PARAMETER Report
    Write and print report.md, the project manager overview: milestones, requirements,
    questions, decisions, work and story points, quality, risks. -Build writes it too.

.PARAMETER Compact
    Move Done tasks and Closed questions that no unfinished task waits for, and that nobody
    has locked, to archive/done/. They stay readable there as the team's history.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Build -Overview

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -Lock T-006 -Owner backend-developer-1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .team/check-decisions.ps1 -New question -From architect -To user -Title "How many years will the system be in use?"
#>
[CmdletBinding()]
param(
    [string]$TeamFolder = '',
    [switch]$Build,
    [switch]$Overview,
    [switch]$SaveOk,
    [string]$Baseline,
    [ValidateSet('task', 'question', 'decision', 'requirement')]
    [string]$New,
    [string]$Title = '',
    [string]$Owner = '',
    [string]$From = '',
    [string]$To = '',
    [string]$WaitsFor = '',
    [string]$For = '',
    [string]$Lock,
    [string]$Unlock,
    [switch]$Force,
    [int]$LockMinutes = 30,
    [int]$SilenceMinutes = 5,
    [switch]$Migrate,
    [switch]$Compact,
    [string]$Restore,
    [string]$UpdateFromSkill,
    [string]$Answer,
    [string]$Text = '',
    [string]$SetStatus,
    [string]$Reopen,
    [ValidateSet('finish', 'now')]
    [string]$Stop,
    [switch]$Resume,
    [switch]$Unblock,
    [switch]$Report,
    [switch]$Standup
)

$ErrorActionPreference = 'Stop'
if (-not $TeamFolder) { $TeamFolder = $PSScriptRoot }
$script:errors = New-Object 'System.Collections.Generic.List[string]'
$script:warnings = New-Object 'System.Collections.Generic.List[string]'
# What the orchestrator must do before anyone else continues; "How to proceed" puts it first.
$script:orchestratorWork = New-Object 'System.Collections.Generic.List[string]'
$script:toUnblock = New-Object 'System.Collections.Generic.List[string]'
$script:readyPullRequests = New-Object 'System.Collections.Generic.List[string]'
$script:userSteps = New-Object 'System.Collections.Generic.List[string]'
$script:notNow = @{}
# Set when an item is missing: then no automatic checked copy is saved, so it cannot replace a good one.
$script:somethingLost = $false
$utf8 = New-Object System.Text.UTF8Encoding($false)

$kinds = [ordered]@{
    task        = @{ Folder = 'tasks'; Prefix = 'T'; Statuses = @('Open', 'In progress', 'Done', 'Blocked'); Required = @('id', 'title', 'owner', 'status', 'created') }
    question    = @{ Folder = 'questions'; Prefix = 'Q'; Statuses = @('Open', 'Closed'); Required = @('id', 'title', 'from', 'to', 'status', 'created') }
    decision    = @{ Folder = 'decisions'; Prefix = 'D'; Statuses = @('Proposed', 'Accepted', 'Rejected', 'Superseded'); Required = @('id', 'title', 'who', 'status', 'date') }
    requirement = @{ Folder = 'requirements'; Prefix = 'REQ'; Statuses = @('Proposed', 'Approved', "Won't"); Required = @('id', 'title', 'type', 'priority', 'status') }
}
$documents = @('project.md', 'plan.md', 'analysis.md', 'architecture.md', 'design.md', 'testing.md')
# A long document may move its details into linked files in a folder of the same name
# (architecture.md -> architecture/database.md). Those files are documents too: checked, copied
# and restored like the others.
$documentFolders = @($documents | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_) })
$agentTypes = @('architect', 'security-officer', 'backend-developer', 'frontend-developer', 'code-reviewer', 'sanity-agent', 'tester', 'analyst', 'designer', 'team-reporter')
# The longest title -New accepts for a task, decision or requirement, and the length tables show.
$maxTitle = 80

function Add-Problem([string]$Kind, [string]$Where, [string]$Text) {
    $line = '{0,-8} {1}  {2}' -f $Kind, $Where, $Text
    if ($Kind -eq 'ERROR') { $script:errors.Add($line) } else { $script:warnings.Add($line) }
}

function Read-Lines([string]$Path) {
    return [System.IO.File]::ReadAllLines((Resolve-Path -LiteralPath $Path).Path, [System.Text.Encoding]::UTF8)
}

function Write-TextFile([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText($Path, $Text, $utf8)
}

function Format-Id([string]$Prefix, [int]$Number) { return '{0}-{1:D3}' -f $Prefix, $Number }

function Get-IdNumber([string]$Id) {
    if ($Id -match '-(\d+)$') { return [int]$Matches[1] }
    return 0
}

# Escapes text for a cell of a generated table.
function Format-Cell([string]$Text) {
    if (-not $Text) { return '-' }
    return ($Text -replace '\|', '/' -replace '\s+', ' ').Trim()
}

# A title for a table of the overview: at most $maxTitle characters, cut at a word; the item
# file has the whole title.
function Format-TitleCell([string]$Text) {
    $t = Format-Cell $Text
    if ($t.Length -le $maxTitle) { return $t }
    $cut = $t.Substring(0, $maxTitle - 3)
    $space = $cut.LastIndexOf(' ')
    if ($space -gt 40) { $cut = $cut.Substring(0, $space) }
    return $cut.TrimEnd(' ', ',', ';', ':', '(') + '...'
}

# "tasks/T-006.md" for an item file (folder and file name, whatever form the full path has).
function Get-RelativePath([string]$Path) {
    $parts = $Path -split '[\\/]'
    return ($parts[-2] + '/' + $parts[-1])
}

# --- Item files ------------------------------------------------------------------------------

# Reads an item file: its header fields (lower-case keys) and its body.
function Read-Item([string]$Path, [string]$Kind) {
    $item = [pscustomobject]@{
        Kind = $Kind; Path = $Path; File = [System.IO.Path]::GetFileNameWithoutExtension($Path)
        Fields = @{}; Body = ''; HasHeader = $false; Empty = $false
    }
    $lines = @(Read-Lines $Path)
    if (@($lines | Where-Object { $_.Trim() }).Count -eq 0) { $item.Empty = $true; return $item }
    if ($lines[0].Trim() -ne '---') { return $item }
    $end = -1
    for ($i = 1; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -eq '---') { $end = $i; break } }
    if ($end -lt 0) { return $item }
    $item.HasHeader = $true
    for ($i = 1; $i -lt $end; $i++) {
        if ($lines[$i] -match '^\s*([A-Za-z][A-Za-z-]*)\s*:\s*(.*)$') { $item.Fields[$Matches[1].ToLower()] = $Matches[2].Trim() }
    }
    if ($end + 1 -lt $lines.Count) { $item.Body = ($lines[($end + 1)..($lines.Count - 1)] -join "`n") }
    return $item
}

function Get-Items([string]$Folder) {
    $items = New-Object 'System.Collections.Generic.List[object]'
    foreach ($kind in $kinds.Keys) {
        $dir = Join-Path $Folder $kinds[$kind].Folder
        if (-not (Test-Path -LiteralPath $dir)) { continue }
        foreach ($f in @(Get-ChildItem -LiteralPath $dir -Filter '*.md' -File | Sort-Object Name)) {
            if ($f.Name -notmatch ('^' + $kinds[$kind].Prefix + '-\d+\.md$')) { continue }
            $items.Add((Read-Item $f.FullName $kind))
        }
    }
    return $items
}

# The documents in folders under $Root: reviews/<name>.md and <document>/<name>.md
# (architecture/database.md), as paths relative to $Root.
function Get-FolderDocuments([string]$Root) {
    $names = New-Object 'System.Collections.Generic.List[string]'
    foreach ($folder in @(@('reviews') + $documentFolders)) {
        $dir = Join-Path $Root $folder
        if (Test-Path -LiteralPath $dir) { foreach ($f in @(Get-ChildItem -LiteralPath $dir -Filter '*.md' -File | Sort-Object Name)) { $names.Add("$folder/$($f.Name)") } }
    }
    return $names
}

# Lines with text of a document; with $WithFolder also those of its folder (architecture/).
function Get-TextLineCount([string]$Root, [string]$Name, [bool]$WithFolder) {
    $count = 0
    $p = Join-Path $Root $Name
    if (Test-Path -LiteralPath $p) { $count += @(Read-Lines $p | Where-Object { $_.Trim() }).Count }
    if ($WithFolder) {
        $dir = Join-Path $Root ([System.IO.Path]::GetFileNameWithoutExtension($Name))
        if (Test-Path -LiteralPath $dir) { foreach ($f in @(Get-ChildItem -LiteralPath $dir -Filter '*.md' -File)) { $count += @(Read-Lines $f.FullName | Where-Object { $_.Trim() }).Count } }
    }
    return $count
}

# The documents that exist: project.md and the others, plus reviews/<name>.md and the files in
# the documents' folders (architecture/<name>.md).
function Get-DocumentNames {
    $names = New-Object 'System.Collections.Generic.List[string]'
    foreach ($d in $documents) { if (Test-Path -LiteralPath (Join-Path $TeamFolder $d)) { $names.Add($d) } }
    foreach ($n in @(Get-FolderDocuments $TeamFolder)) { $names.Add($n) }
    return $names
}

# Ids of finished items that compaction moved to archive/done/ (they count as Done or Closed).
function Get-ArchivedDone {
    $ids = @{}
    $dir = Join-Path (Join-Path $TeamFolder 'archive') 'done'
    if (-not (Test-Path -LiteralPath $dir)) { return $ids }
    foreach ($f in @(Get-ChildItem -LiteralPath $dir -Recurse -Filter '*.md' -File)) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($f.Name)
        if ($name -match '^(T|Q|D|REQ)-\d+$') { $ids[$name] = $true }
    }
    return $ids
}

# Ids the orchestrator removed on purpose, listed in archive/done/removed.md as "- T-003: reason"
# (for example items an older version of the skill compacted away). They count as finished.
function Get-RemovedIds {
    $ids = @{}
    $file = Join-Path (Join-Path (Join-Path $TeamFolder 'archive') 'done') 'removed.md'
    if (-not (Test-Path -LiteralPath $file)) { return $ids }
    foreach ($l in (Read-Lines $file)) {
        if ($l -notmatch '^\s*[-*]\s') { continue }
        foreach ($m in [regex]::Matches($l, '\b(T|Q|D|REQ)-(\d+)\b')) { $ids[(Format-Id $m.Groups[1].Value ([int]$m.Groups[2].Value))] = $true }
    }
    return $ids
}

# Archived and removed ids together: items that are gone from the folders on purpose.
function Get-GoneOnPurpose {
    $ids = Get-ArchivedDone
    foreach ($id in @((Get-RemovedIds).Keys)) { $ids[$id] = $true }
    return $ids
}

function Get-Field($Item, [string]$Name) {
    if ($Item.Fields.ContainsKey($Name)) { return $Item.Fields[$Name] }
    return ''
}

# Reads a waits-for field: task, question and decision ids, and "user".
function Get-ItemWaits([string]$Text) {
    $waits = @{ Tasks = @(); Questions = @(); Decisions = @(); User = $false }
    if (-not $Text) { return $waits }
    $waits.Tasks = @([regex]::Matches($Text, '(?i)\bT-(\d+)\b') | ForEach-Object { Format-Id 'T' ([int]$_.Groups[1].Value) } | Select-Object -Unique)
    $waits.Questions = @([regex]::Matches($Text, '(?i)\bQ-(\d+)\b') | ForEach-Object { Format-Id 'Q' ([int]$_.Groups[1].Value) } | Select-Object -Unique)
    $waits.Decisions = @([regex]::Matches($Text, '(?i)\bD-(\d+)\b') | ForEach-Object { Format-Id 'D' ([int]$_.Groups[1].Value) } | Select-Object -Unique)
    $waits.User = ($Text -match '(?i)\buser\b')
    return $waits
}

# --- Locks -----------------------------------------------------------------------------------

function Get-LockPath([string]$Name) {
    # A document in a folder (architecture/build-tasks.md, .team/reviews/sanity-reviews.md) is
    # locked by its file name, so the path and the file name lock the same file.
    $Name = $Name.Trim() -replace '\\', '/'
    if ($Name.Contains('/')) { $Name = $Name.Substring($Name.LastIndexOf('/') + 1) }
    if ($Name -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]*$') { throw "'$Name' is not a valid item id or document name." }
    return (Join-Path (Join-Path $TeamFolder 'locks') "$Name.lock")
}

function Read-Lock([string]$Path) {
    $info = @{ Owner = ''; Time = [datetime]::MinValue }
    try {
        foreach ($l in (Read-Lines $Path)) {
            if ($l -match '^owner:\s*(.+)$') { $info.Owner = $Matches[1].Trim() }
            if ($l -match '^time:\s*(.+)$') { $info.Time = [datetime]::Parse($Matches[1].Trim(), [Globalization.CultureInfo]::InvariantCulture) }
        }
    } catch { }
    return $info
}

function Lock-TeamFile([string]$Name, [string]$Who) {
    if (-not $Who) { throw 'Give -Owner: your instance name.' }
    $path = Get-LockPath $Name
    New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
    $now = Get-Date
    $content = "owner: $Who`ntime: $($now.ToString('yyyy-MM-ddTHH:mm:ss', [Globalization.CultureInfo]::InvariantCulture))`n"
    for ($attempt = 0; $attempt -lt 2; $attempt++) {
        try {
            # CreateNew fails when the lock file exists: only one agent can hold it.
            $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
            $bytes = $utf8.GetBytes($content); $stream.Write($bytes, 0, $bytes.Length); $stream.Close()
            Write-Output "Locked $Name for $Who"
            $script:exitCode = 0; return
        } catch [System.IO.IOException] {
            $held = Read-Lock $path
            if ($held.Owner -eq $Who) { Write-TextFile $path $content; Write-Output "Locked $Name for $Who (you already held it)"; $script:exitCode = 0; $script:alreadyHeld = $true; return }
            $age = ($now - $held.Time).TotalMinutes
            if ($age -ge $LockMinutes) {
                Remove-Item -LiteralPath $path -Force
                Write-Output ("Took over the stale lock of {0} on {1} ({2:N0} minutes old)" -f $held.Owner, $Name, $age)
                continue
            }
            Write-Output ("LOCKED   {0} is locked by {1} since {2:HH:mm}: work on something else first and try again later" -f $Name, $held.Owner, $held.Time)
            $script:exitCode = 2; return
        }
    }
    $script:exitCode = 2
}

function Unlock-TeamFile([string]$Name, [string]$Who) {
    $path = Get-LockPath $Name
    if (-not (Test-Path -LiteralPath $path)) { Write-Output "$Name was not locked"; $script:exitCode = 0; return }
    $held = Read-Lock $path
    if ($held.Owner -ne $Who -and -not $Force) {
        Write-Output "LOCKED   $Name is locked by $($held.Owner), not by '$Who': not released (the orchestrator can use -Force)"
        $script:exitCode = 2; return
    }
    Remove-Item -LiteralPath $path -Force
    Write-Output "Unlocked $Name"
    $script:exitCode = 0
}

# Locks several files in one call ("T-006, Q-024, architecture.md"): all of them, or none.
function Lock-TeamFiles([string]$Names, [string]$Who) {
    $taken = New-Object 'System.Collections.Generic.List[string]'
    foreach ($n in @($Names -split '\s*,\s*' | Where-Object { $_ })) {
        $script:alreadyHeld = $false
        Lock-TeamFile $n $Who
        if ($script:exitCode -ne 0) {
            foreach ($t in $taken) { Unlock-TeamFile $t $Who | Out-Null }
            if ($taken.Count -gt 0) { Write-Output ('Released again: {0}. Nothing new is locked; work on something else first and try again later' -f ($taken -join ', ')) }
            $script:exitCode = 2
            return
        }
        if (-not $script:alreadyHeld) { $taken.Add($n) }
    }
    $script:exitCode = 0
}

# Releases several locks in one call; "all" releases every lock of -Owner.
function Unlock-TeamFiles([string]$Names, [string]$Who) {
    if ($Names -eq 'all') {
        if (-not $Who) { throw 'Give -Owner: your instance name.' }
        $mine = @(Get-Locks | Where-Object { $_ -and $_.Owner -eq $Who } | ForEach-Object { $_.Name })
        if ($mine.Count -eq 0) { Write-Output "$Who holds no locks"; $script:exitCode = 0; return }
        $Names = $mine -join ','
    }
    $code = 0
    foreach ($n in @($Names -split '\s*,\s*' | Where-Object { $_ })) {
        Unlock-TeamFile $n $Who
        if ($script:exitCode -ne 0) { $code = $script:exitCode }
    }
    $script:exitCode = $code
}

function Get-Locks {
    $list = New-Object 'System.Collections.Generic.List[object]'
    $dir = Join-Path $TeamFolder 'locks'
    if (-not (Test-Path -LiteralPath $dir)) { return $list }
    foreach ($f in @(Get-ChildItem -LiteralPath $dir -Filter '*.lock' -File | Sort-Object Name)) {
        $held = Read-Lock $f.FullName
        $list.Add([pscustomobject]@{ Name = $f.BaseName; Owner = $held.Owner; Time = $held.Time; Minutes = ((Get-Date) - $held.Time).TotalMinutes })
    }
    return $list
}

# --- New item --------------------------------------------------------------------------------

function New-TeamItem {
    if (-not $Title) { throw 'Give a -Title for the new item.' }
    # Short titles keep decisions.md small, which every agent reads at the start of every run.
    if ($New -ne 'question' -and $Title.Length -gt $maxTitle) {
        [Console]::Out.WriteLine(('REFUSED: nothing created. The title has {0} characters; at most {1}. Give a short title and put the details in -Text "<details>" (written below the title).' -f $Title.Length, $maxTitle))
        exit 1
    }
    $details = ''
    if ($Text) { $details = "`n`n" + $Text.Trim() }
    $info = $kinds[$New]
    $dir = Join-Path $TeamFolder $info.Folder
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $today = Get-Date -Format 'yyyy-MM-dd'

    # The next number counts the archive and the register of issued ids too, so a number is
    # never used twice, not even when its file was lost.
    $highest = 0
    $searchRoots = @($dir) + @(Get-ChildItem -LiteralPath (Join-Path $TeamFolder 'archive') -Directory -ErrorAction SilentlyContinue |
            ForEach-Object { Join-Path $_.FullName $info.Folder } | Where-Object { Test-Path -LiteralPath $_ })
    foreach ($root in $searchRoots) {
        foreach ($f in @(Get-ChildItem -LiteralPath $root -Filter "$($info.Prefix)-*.md" -File -ErrorAction SilentlyContinue)) {
            $n = Get-IdNumber ([System.IO.Path]::GetFileNameWithoutExtension($f.Name))
            if ($n -gt $highest) { $highest = $n }
        }
    }
    $registered = Get-IssuedIds
    foreach ($id in @($registered.Keys | Where-Object { $_ -match ('^' + $info.Prefix + '-\d+$') })) {
        $n = Get-IdNumber $id
        if ($n -gt $highest) { $highest = $n }
    }

    switch ($New) {
        'task' {
            if (-not $Owner) { throw 'Give the -Owner (instance) of the task.' }
            # Only work within the approved requirements (RULES.md, rule 14).
            $problem = Get-TraceProblem $For @(Get-Items $TeamFolder)
            if ($problem) {
                [Console]::Out.WriteLine(('REFUSED: no task created. The new task {0}: it is not within the approved requirements (RULES.md, rule 14). Give -For REQ-nnn (an approved requirement), D-nnn (a decision whose requirements: header names the requirements it serves), BUG-n (its row in testing.md names the requirement) or team (team-internal work, no product change). When it is needed outside the requirements, ask the user first: -New question -To user, with what it is, why it is needed and what it costs.' -f $problem))
                exit 1
            }
            # One task per item and role: a next step, a fix round or a re-review is a new round
            # of the role's task for that item (-Reopen), not a new task.
            if ($For -and $For -ne 'team') {
                $role = $Owner -replace '-\d+$', ''
                $same = @(Get-Items $TeamFolder | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'for') -eq $For -and ((Get-Field $_ 'owner') -replace '-\d+$', '') -eq $role } | Sort-Object { Get-IdNumber $_.File } | Select-Object -Last 1)
                if ($same.Count -gt 0) {
                    $s = $same[0]
                    [Console]::Out.WriteLine(('REFUSED: no task created. {0} ({1}, {2}) is already the task of the {3} for {4}: one task per item and role. Add the new work to it as a new round: -Reopen {0} -Text "<what to do now>"' -f $s.File, (Get-Field $s 'owner'), (Get-Field $s 'status'), $role, $For))
                    exit 1
                }
            }
            $status = 'Open'; if ($WaitsFor) { $status = 'Blocked' }
            $header = @("title: $Title", "owner: $Owner", "status: $status", "waits-for: $WaitsFor", "for: $For", "created: $today")
            $body = "## Task`n`n$Title$details`n`n## Plan`n`n(Written by the owner before starting: the steps, what is needed, and every question raised, or ""no questions"".)`n`n## Report`n`n(Written by the owner when Done or Blocked: what was done, decisions added, questions raised, and ""Unblocks: T-..."".)`n"
        }
        'question' {
            if (-not $From -or -not $To) { throw 'Give -From (who asks) and -To (a role, an instance or user).' }
            $header = @("title: $Title", "from: $From", "to: $To", 'status: Open', "created: $today")
            $body = "## Question`n`n$Title$details`n`n## Answer`n`n(Written by the addressee. For a question to the user, the orchestrator writes the user's answer.)`n"
        }
        'decision' {
            if (-not $Owner) { throw 'Give the -Owner (instance) who takes the decision.' }
            $header = @("title: $Title", "who: $Owner", 'status: Proposed', "date: $today", "requirements: $For", 'files: ')
            $body = "## Decision`n`n$Title$details`n`n## Why`n`n(The reasoning, including the alternatives rejected.)`n"
        }
        'requirement' {
            $header = @("title: $Title", 'type: Functional', 'priority: Must', 'status: Proposed', 'source: ')
            $body = "## Requirement`n`n$Title$details`n`n## Acceptance criteria`n`n- (Testable criteria, Given / When / Then.)`n"
        }
    }

    for ($n = $highest + 1; $n -lt $highest + 1000; $n++) {
        $id = Format-Id $info.Prefix $n
        $path = Join-Path $dir "$id.md"
        try {
            # CreateNew fails when another agent took this number a moment ago: then try the next one.
            $stream = [System.IO.File]::Open($path, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write)
        } catch [System.IO.IOException] { continue }
        $text = "---`nid: $id`n" + ($header -join "`n") + "`n---`n`n" + $body
        $bytes = $utf8.GetBytes($text)
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Close()
        $who = $Owner; if ($New -eq 'question') { $who = "$From -> $To" }
        Add-IssuedId $id $who $Title
        Write-Output "Created $($info.Folder)/$id.md"
        return
    }
    throw 'Could not find a free number.'
}

# --- Register of issued ids ------------------------------------------------------------------

# ids.log in the team folder lists every id ever issued, one line each:
#   T-026 | 2026-11-09 14:54 | orchestrator | Record the user's answers ...
# -New writes it; -Build adds ids of items that were created by hand. Lines are never removed,
# so a number is never issued twice and a lost item is always noticed.
function Get-IssuedIds {
    $ids = [ordered]@{}
    $file = Join-Path $TeamFolder 'ids.log'
    if (-not (Test-Path -LiteralPath $file)) { return $ids }
    foreach ($l in (Read-Lines $file)) {
        if ($l -match '^\s*((?:T|Q|D|REQ)-\d+)\s*\|\s*([^|]*)\|\s*([^|]*)\|\s*(.*)$') {
            if (-not $ids.Contains($Matches[1])) { $ids[$Matches[1]] = [pscustomobject]@{ Time = $Matches[2].Trim(); Who = $Matches[3].Trim(); Title = $Matches[4].Trim() } }
        }
    }
    return $ids
}

function Add-IssuedId([string]$Id, [string]$Who, [string]$Text) {
    $file = Join-Path $TeamFolder 'ids.log'
    $line = '{0} | {1} | {2} | {3}' -f $Id, (Get-Date -Format 'yyyy-MM-dd HH:mm'), $Who, (($Text -replace '\s+', ' ').Trim())
    if (-not (Test-Path -LiteralPath $file)) {
        $line = "# Every id ever issued in this team folder. Never change or remove a line.`n" + $line
    }
    # Several agents may add a line at the same moment: retry when the file is busy.
    for ($try = 0; $try -lt 20; $try++) {
        try { [System.IO.File]::AppendAllText($file, $line + "`n", $utf8); return } catch [System.IO.IOException] { Start-Sleep -Milliseconds 100 }
    }
    Write-Output "WARNING  could not write $Id to ids.log: add it by hand"
}

# Adds the ids of item files that are not registered yet (created by hand, or before ids.log).
function Register-ExistingIds($Items) {
    $known = Get-IssuedIds
    $added = 0
    foreach ($it in @($Items | Where-Object { $_.HasHeader } | Sort-Object { $_.Kind }, { Get-IdNumber $_.File })) {
        if ($known.Contains($it.File)) { continue }
        $who = (Get-Field $it 'owner'); if (-not $who) { $who = (Get-Field $it 'who') }
        if ($it.Kind -eq 'question') { $who = '{0} -> {1}' -f (Get-Field $it 'from'), (Get-Field $it 'to') }
        Add-IssuedId $it.File $who (Get-Field $it 'title')
        $added++
    }
    if ($added -gt 0) { Write-Output "Registered $added item(s) in ids.log" }
}

# --- Migration from the old single decisions.md -----------------------------------------------

# Splits a table row into cells; a | inside a code span is not a separator.
function Get-Cells([string]$Line) {
    $spans = New-Object 'System.Collections.Generic.List[string]'
    $clean = [regex]::Replace($Line, '`[^`]*`', { param($m) $spans.Add($m.Value); '@@' + ($spans.Count - 1) + '@@' })
    $clean = $clean.Trim()
    if ($clean.StartsWith('|')) { $clean = $clean.Substring(1) }
    if ($clean.EndsWith('|')) { $clean = $clean.Substring(0, $clean.Length - 1) }
    $result = @()
    foreach ($c in @([regex]::Split($clean, '(?<!\\)\|'))) {
        $c = $c.Trim()
        for ($k = 0; $k -lt $spans.Count; $k++) { $c = $c.Replace("@@$k@@", $spans[$k]) }
        $result += $c
    }
    return $result
}

# Reads what an old task row waits for ("Waits for: task 5, question 24", "Blocked until tasks 6 to 8").
function Get-LegacyWaits([string]$Text) {
    $ids = New-Object 'System.Collections.Generic.List[string]'
    $user = $false
    foreach ($segment in [regex]::Matches($Text, '(?i)(waits for:?|blocked until|blocked as)([^|]*)')) {
        $s = $segment.Groups[2].Value
        if ($s -match '(?i)\buser\b') { $user = $true }
        foreach ($group in [regex]::Matches($s, '(?i)\b(tasks?|questions?)\s+((?:\d+\s*(?:,|and|to|-|\u2013)?\s*)+)')) {
            if ($group.Groups[1].Value -match '(?i)^task') { $prefix = 'T' } else { $prefix = 'Q' }
            foreach ($n in [regex]::Matches($group.Groups[2].Value, '(\d+)\s*(?:to|-|\u2013)\s*(\d+)|(\d+)')) {
                if ($n.Groups[1].Success) {
                    for ($k = [int]$n.Groups[1].Value; $k -le [int]$n.Groups[2].Value; $k++) { $id = Format-Id $prefix $k; if (-not $ids.Contains($id)) { $ids.Add($id) } }
                } else { $id = Format-Id $prefix ([int]$n.Groups[3].Value); if (-not $ids.Contains($id)) { $ids.Add($id) } }
            }
        }
        foreach ($d in [regex]::Matches($s, '#(\d+)')) { $id = Format-Id 'D' ([int]$d.Groups[1].Value); if (-not $ids.Contains($id)) { $ids.Add($id) } }
    }
    if ($user) { $ids.Add('user') }
    return ($ids -join ', ')
}

function Get-ShortTitle([string]$Text) {
    $t = ($Text -replace '\s+', ' ').Trim()
    if ($t.Length -le 90) { return $t }
    $cut = $t.Substring(0, 90)
    $dot = $cut.LastIndexOf('. ')
    if ($dot -gt 30) { return $cut.Substring(0, $dot + 1) }
    return $cut.TrimEnd() + '...'
}

function Save-LegacyDecision($Decision) {
    $fields = @{ date = ''; who = ''; status = 'Proposed'; files = '' }
    $parts = [ordered]@{ Decision = ''; Why = ''; 'Replaced along the way' = '' }
    $current = $null
    foreach ($l in ($Decision.Text -split "`n")) {
        if ($l -match '^- \*\*(Date|Who|Status|Decision|Why|File\(s\)|Replaced along the way):\*\*\s*(.*)$') {
            $key = $Matches[1]; $value = $Matches[2].Trim()
            switch ($key) {
                'Date' { $fields.date = $value; $current = $null }
                'Who' { $fields.who = $value; $current = $null }
                'Status' { $fields.status = $value; $current = $null }
                'File(s)' { $fields.files = $value; $current = 'Files' }
                default { $current = $key; $parts[$key] = $value }
            }
        } elseif ($current -and $current -ne 'Files' -and $l.Trim()) { $parts[$current] += "`n" + $l.Trim() }
        elseif ($current -eq 'Files' -and $l.Trim()) { $fields.files += ' ' + $l.Trim() }
    }
    $text = "---`nid: $($Decision.Id)`ntitle: $($Decision.Title)`nwho: $($fields.who)`nstatus: $($fields.status)`ndate: $($fields.date)`nfiles: $($fields.files.Trim())`n---`n`n## Decision`n`n$($parts.Decision)`n`n## Why`n`n$($parts.Why)`n"
    if ($parts['Replaced along the way']) { $text += "`n## Replaced along the way`n`n$($parts['Replaced along the way'])`n" }
    Write-TextFile (Join-Path (Join-Path $TeamFolder 'decisions') "$($Decision.Id).md") $text
}

function Invoke-Migration {
    $old = Join-Path $TeamFolder 'decisions.md'
    if (-not (Test-Path -LiteralPath $old)) { throw "There is no $old to migrate." }
    $lines = @(Read-Lines $old)
    if (@($lines -match '^## Task board').Count -eq 0) { throw 'decisions.md has no Task board: it is not the old layout.' }
    foreach ($k in $kinds.Keys) {
        $dir = Join-Path $TeamFolder $kinds[$k].Folder
        if ((Test-Path -LiteralPath $dir) -and @(Get-ChildItem -LiteralPath $dir -Filter '*.md').Count -gt 0) { throw "$dir already has item files: the folder is already migrated." }
    }

    $stamp = Get-Date -Format 'yyyy-MM-dd-HHmm'
    $archive = Join-Path (Join-Path $TeamFolder 'archive') "$stamp-before-migration"
    New-Item -ItemType Directory -Path $archive -Force | Out-Null
    Copy-Item -LiteralPath $old -Destination $archive
    foreach ($k in $kinds.Keys) { New-Item -ItemType Directory -Path (Join-Path $TeamFolder $kinds[$k].Folder) -Force | Out-Null }
    New-Item -ItemType Directory -Path (Join-Path $TeamFolder 'reviews') -Force | Out-Null

    $skipped = New-Object 'System.Collections.Generic.List[string]'
    $notesOut = New-Object 'System.Collections.Generic.List[string]'
    $docs = [ordered]@{}
    foreach ($n in @('project.md', 'analysis.md', 'architecture.md', 'design.md', 'testing.md', 'reviews/security-assessments.md', 'reviews/sanity-reviews.md')) { $docs[$n] = New-Object 'System.Text.StringBuilder' }
    $targets = @{ 'Team' = 'project.md'; 'Customer request' = 'project.md'; 'Project environment' = 'project.md'
        'Analysis' = 'analysis.md'; 'Requirements' = 'architecture.md'; 'Architecture' = 'architecture.md'; 'Design' = 'design.md'
        'Testing' = 'testing.md'; 'Security assessments' = 'reviews/security-assessments.md'; 'Sanity reviews' = 'reviews/sanity-reviews.md' }
    $tableSections = @('Task board', 'Open questions', 'Requirements')

    $title = 'Project'
    if ($lines[0] -match '^# (?:Team decisions: )?(.+)$') { $title = $Matches[1].Trim() }
    [void]$docs['project.md'].AppendLine("# Project: $title").AppendLine()
    foreach ($l in $lines) { if ($l -match '^\*\*Last checked against the skill:\*\*') { [void]$docs['project.md'].AppendLine($l).AppendLine() } }
    $approved = @($lines -match '^\*\*Approved by the user:\*\*\s*\d{4}-').Count -gt 0

    $tasks = [ordered]@{}; $questions = [ordered]@{}; $notes = @{}
    $section = ''; $inTable = $false
    $decision = $null; $decisionText = $null; $rejected = $false
    $i = 0
    while ($i -lt $lines.Count) {
        $line = $lines[$i]
        $lineNumber = $i + 1
        if ($line -match '^## (.+)$') {
            if ($decision) { $decision.Text = $decisionText.ToString(); Save-LegacyDecision $decision; $decision = $null }
            $section = $Matches[1].Trim(); $inTable = $false; $i++
            if ($targets.ContainsKey($section)) { [void]$docs[$targets[$section]].AppendLine($line) }
            continue
        }
        if ($section -eq '' -or -not ($targets.ContainsKey($section) -or $section -in @('Task board', 'Open questions', 'Decisions'))) { $i++; continue }

        $isRow = $line.TrimStart().StartsWith('|')
        $isTableSection = $tableSections -contains $section
        if (-not $isRow) {
            if ($inTable -and $line.Trim() -and $isTableSection) { $skipped.Add("decisions.md:$lineNumber ($section) $line"); $i++; continue }
            $inTable = $false
        }
        if ($isRow -and -not $inTable) {
            $inTable = $true; $i++
            if (-not $isTableSection -and $section -ne 'Decisions') { [void]$docs[$targets[$section]].AppendLine($line) }
            continue
        }
        if ($isRow -and $line -match '^\s*\|[\s:|-]+\|\s*$') {
            if (-not $isTableSection -and $section -ne 'Decisions') { [void]$docs[$targets[$section]].AppendLine($line) }
            $i++; continue
        }

        if ($isRow -and $section -eq 'Task board') {
            $c = Get-Cells $line
            if ($c.Count -lt 5 -or $c[0] -notmatch '^\d+$') { $skipped.Add("decisions.md:$lineNumber (Task board) $line"); $i++; continue }
            $id = Format-Id 'T' ([int]$c[0])
            if ($tasks.Contains($id)) { $skipped.Add("decisions.md:$lineNumber (Task board, second row with number $($c[0])) $line"); $i++; continue }
            $report = ''; if ($c.Count -gt 5) { $report = $c[5] }
            $tasks[$id] = [pscustomobject]@{ Id = $id; Created = $c[1]; Owner = $c[2]; Text = $c[3]; Status = $c[4]; Report = $report; Waits = (Get-LegacyWaits ($c[3] + ' | ' + $report)) }
            $i++; continue
        }
        if ($isRow -and $section -eq 'Open questions') {
            $c = Get-Cells $line
            if ($c.Count -lt 7 -or $c[0] -notmatch '^\d+$') { $skipped.Add("decisions.md:$lineNumber (Open questions) $line"); $i++; continue }
            $number = [int]$c[0]
            $id = Format-Id 'Q' $number
            if ($questions.Contains($id)) {
                $next = 1 + (@($questions.Keys | ForEach-Object { Get-IdNumber $_ }) | Measure-Object -Maximum).Maximum
                $id = Format-Id 'Q' $next
                $notesOut.Add("question $number appears twice: the second one (from $($c[2]) to $($c[3])) became $id")
            }
            $status = $c[6]; if ($status -eq 'Answered') { $status = 'Closed' }
            $questions[$id] = [pscustomobject]@{ Id = $id; Created = $c[1]; From = $c[2]; To = $c[3]; Text = $c[4]; Answer = $c[5]; Status = $status }
            $i++; continue
        }
        if ($isRow -and $section -eq 'Requirements') {
            $c = Get-Cells $line
            if ($c[0] -notmatch '^REQ-(\d+)$') { $skipped.Add("decisions.md:$lineNumber (Requirements) $line"); $i++; continue }
            $id = Format-Id 'REQ' ([int]$Matches[1])
            $status = 'Proposed'; if ($approved) { $status = 'Approved' }
            $priority = 'Must'; if ($c.Count -gt 5) { $priority = $c[5] }
            if ($priority -match "(?i)won'?t") { $status = "Won't" }
            $acceptance = ''; if ($c.Count -gt 3) { $acceptance = $c[3] }
            $source = ''; if ($c.Count -gt 4) { $source = $c[4] }
            $text = "---`nid: $id`ntitle: $(Get-ShortTitle $c[2])`ntype: $($c[1])`npriority: $priority`nstatus: $status`nsource: $source`n---`n`n## Requirement`n`n$($c[2])`n`n## Acceptance criteria`n`n$acceptance`n"
            Write-TextFile (Join-Path (Join-Path $TeamFolder 'requirements') "$id.md") $text
            $i++; continue
        }

        # Plan and note paragraphs below the Task board and Open questions belong to their task or question.
        if ($section -in @('Task board', 'Open questions') -and $line -match '^\*\*(Plan task|Note)\s+(\d+)\b') {
            $prefix = 'T'; if ($section -eq 'Open questions' -and $Matches[1] -eq 'Note') { $prefix = 'Q' }
            $key = Format-Id $prefix ([int]$Matches[2])
            $block = New-Object 'System.Text.StringBuilder'
            while ($i -lt $lines.Count -and $lines[$i].Trim()) { [void]$block.AppendLine($lines[$i]); $i++ }
            if (-not $notes.ContainsKey($key)) { $notes[$key] = '' }
            $notes[$key] += $block.ToString() + "`n"
            continue
        }
        if ($section -in @('Task board', 'Open questions')) { $i++; continue }

        if ($section -eq 'Decisions') {
            if ($line -match '^### (\d+)\.\s*(.+)$') {
                if ($decision) { $decision.Text = $decisionText.ToString(); Save-LegacyDecision $decision }
                $decision = [pscustomobject]@{ Id = (Format-Id 'D' ([int]$Matches[1])); Title = $Matches[2].Trim(); Text = '' }
                $decisionText = New-Object 'System.Text.StringBuilder'
                $i++; continue
            }
            if ($line -match '^\*\*Rejected decisions\*\*') {
                if ($decision) { $decision.Text = $decisionText.ToString(); Save-LegacyDecision $decision; $decision = $null }
                $rejected = $true; [void]$docs['project.md'].AppendLine('## Rejected decisions').AppendLine(); $i++; continue
            }
            if ($decision) { [void]$decisionText.AppendLine($line); $i++; continue }
            if ($rejected -and $line.Trim() -and $line -notmatch '^\(added by compaction') { [void]$docs['project.md'].AppendLine($line) }
            $i++; continue
        }

        [void]$docs[$targets[$section]].AppendLine($line)
        $i++
    }
    if ($decision) { $decision.Text = $decisionText.ToString(); Save-LegacyDecision $decision }

    foreach ($t in $tasks.Values) {
        $status = $t.Status
        if ($kinds.task.Statuses -notcontains $status) { $notesOut.Add("$($t.Id) had status '$($t.Status)': set to Open"); $status = 'Open' }
        $plan = ''; if ($notes.ContainsKey($t.Id)) { $plan = $notes[$t.Id] }
        $text = "---`nid: $($t.Id)`ntitle: $(Get-ShortTitle $t.Text)`nowner: $($t.Owner)`nstatus: $status`nwaits-for: $($t.Waits)`ncreated: $($t.Created)`n---`n`n## Task`n`n$($t.Text)`n`n## Plan`n`n$plan`n## Report`n`n$($t.Report)`n"
        Write-TextFile (Join-Path (Join-Path $TeamFolder 'tasks') "$($t.Id).md") $text
    }
    foreach ($q in $questions.Values) {
        $extra = ''; if ($notes.ContainsKey($q.Id)) { $extra = "`n" + $notes[$q.Id] }
        $text = "---`nid: $($q.Id)`ntitle: $(Get-ShortTitle $q.Text)`nfrom: $($q.From)`nto: $($q.To)`nstatus: $($q.Status)`ncreated: $($q.Created)`n---`n`n## Question`n`n$($q.Text)`n`n## Answer`n`n$($q.Answer)`n$extra"
        Write-TextFile (Join-Path (Join-Path $TeamFolder 'questions') "$($q.Id).md") $text
    }
    foreach ($name in $docs.Keys) {
        $content = $docs[$name].ToString().Trim()
        if (-not $content) { continue }
        Write-TextFile (Join-Path $TeamFolder $name) ($content + "`n")
    }
    if ($skipped.Count -gt 0) {
        Write-TextFile (Join-Path $archive 'migration-skipped.md') ("# Lines the migration could not read`n`nRepair them by hand into the item files.`n`n" + (($skipped | ForEach-Object { "- $_" }) -join "`n") + "`n")
    }

    Write-Output ("Migrated: {0} tasks, {1} questions, {2} decisions, {3} requirements. The old file is in {4}." -f $tasks.Count, $questions.Count,
        @(Get-ChildItem -LiteralPath (Join-Path $TeamFolder 'decisions') -Filter 'D-*.md').Count,
        @(Get-ChildItem -LiteralPath (Join-Path $TeamFolder 'requirements') -Filter 'REQ-*.md').Count, $archive)
    foreach ($r in $notesOut) { Write-Output "NOTE     $r" }
    foreach ($s in $skipped) { Write-Output ('SKIPPED  ' + $s.Substring(0, [Math]::Min(160, $s.Length))) }
    if ($skipped.Count -gt 0) { Write-Output "The skipped lines are listed in $archive\migration-skipped.md." }
}

# --- Checks ----------------------------------------------------------------------------------

function Test-Items($Items) {
    $seen = @{}
    foreach ($it in $Items) {
        $where = Get-RelativePath $it.Path
        $info = $kinds[$it.Kind]
        if ($it.Empty) { Add-Problem 'ERROR' $where 'is empty: run -Restore missing'; $script:orchestratorWork.Add("restore $($it.File) (-Restore missing)"); $script:somethingLost = $true; continue }
        if (-not $it.HasHeader) { Add-Problem 'ERROR' $where "has no header between two '---' lines: run -Restore missing"; $script:orchestratorWork.Add("restore $($it.File) (-Restore missing)"); $script:somethingLost = $true; continue }
        $id = Get-Field $it 'id'
        if ($id -ne $it.File) { Add-Problem 'ERROR' $where "id '$id' does not match the file name" }
        if ($seen.ContainsKey($id)) { Add-Problem 'ERROR' $where "id $id is also used by $($seen[$id])" } else { $seen[$id] = $where }
        foreach ($f in $info.Required) { if (-not (Get-Field $it $f)) { Add-Problem 'ERROR' $where "has no '$f'" } }
        $status = Get-Field $it 'status'
        $ok = $info.Statuses -contains $status
        if ($it.Kind -eq 'decision' -and $status -match '^Superseded by D-\d+$') { $ok = $true }
        if ($status -and -not $ok) { Add-Problem 'ERROR' $where ("status '{0}' is not one of: {1}" -f $status, ($info.Statuses -join ', ')) }
        # An item's worktree: the folder must exist, and .worktrees/ must be git-ignored.
        $wt = Get-Field $it 'worktree'
        if ($wt) {
            $repoRoot = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Parent
            if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $wt))) {
                Add-Problem 'WARNING' $where "worktree '$wt' does not exist: create it (git worktree add $wt <branch>) or remove the worktree line"
                $script:orchestratorWork.Add("create or remove the worktree of $($it.File)")
            }
            $ignore = Join-Path $repoRoot '.gitignore'
            if (-not (Test-Path -LiteralPath $ignore) -or -not (@(Read-Lines $ignore | Where-Object { $_.Trim() -match '^/?\.worktrees/?$' }).Count)) {
                Add-Problem 'WARNING' '.gitignore' "does not ignore .worktrees/, while $($it.File) uses a worktree: add the line .worktrees/"
            }
        }
        # The item a task is for: an existing requirement or decision, or a bug (BUG-n in testing.md).
        $for = Get-Field $it 'for'
        if ($it.Kind -eq 'task' -and $for -and (Get-Field $it 'status') -eq 'Done') {
            if ($for -match '^(REQ|D)-\d+$') { if (-not (@($Items | Where-Object { $_.File -eq $for }).Count)) { Add-Problem 'WARNING' $where "is for $for, which does not exist" } }
            elseif ($for -notmatch '^(BUG-\d+|team)$') { Add-Problem 'WARNING' $where "'for: $for' is not a requirement (REQ-nnn), a decision (D-nnn), a bug (BUG-n) or team" }
        }
    }
}

function Test-Waits($Items) {
    $tasks = @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader })
    $byId = @{}
    foreach ($it in $Items) { if ($it.HasHeader) { $byId[(Get-Field $it 'id')] = $it } }
    $archived = Get-GoneOnPurpose
    $script:edges = @{}
    foreach ($t in $tasks) {
        $id = Get-Field $t 'id'
        if ((Get-Field $t 'status') -ne 'Blocked') { continue }
        $w = Get-ItemWaits (Get-Field $t 'waits-for')
        $where = Get-RelativePath $t.Path
        $all = @($w.Tasks) + @($w.Questions) + @($w.Decisions)
        if ($all.Count -eq 0 -and -not $w.User) { Add-Problem 'WARNING' $where "is Blocked but 'waits-for' names nothing"; continue }
        $missing = 0
        foreach ($ref in $all) { if (-not $byId.ContainsKey($ref) -and -not $archived.ContainsKey($ref)) { Add-Problem 'WARNING' $where "waits for $ref, which does not exist"; $missing++ } }
        $script:edges[$id] = @($w.Tasks)
        # A missing item counts as unfinished: it is probably damaged or lost, not done.
        $pending = $missing + @($w.Tasks | Where-Object { $byId.ContainsKey($_) -and (Get-Field $byId[$_] 'status') -ne 'Done' }).Count +
            @($w.Questions | Where-Object { $byId.ContainsKey($_) -and (Get-Field $byId[$_] 'status') -eq 'Open' }).Count +
            @($w.Decisions | Where-Object { $byId.ContainsKey($_) -and (Get-Field $byId[$_] 'status') -eq 'Proposed' }).Count
        if ($all.Count -gt 0 -and $pending -eq 0 -and -not $w.User) { Add-Problem 'WARNING' $where 'waits only for finished work: set it to Open'; $script:toUnblock.Add($id) }
    }

    $script:visiting = @{}; $script:visited = @{}
    $script:cycles = New-Object 'System.Collections.Generic.List[string]'
    foreach ($n in @($script:edges.Keys)) { Find-Cycle $n (New-Object 'System.Collections.Generic.List[string]') }
    foreach ($c in $script:cycles) { Add-Problem 'ERROR' 'tasks' ("deadlock: {0}. Break it first: make the smallest step on one side a task for its role and put it first" -f $c) }

    $active = @($tasks | Where-Object { (Get-Field $_ 'status') -in @('Open', 'In progress') }).Count
    $blocked = @($tasks | Where-Object { (Get-Field $_ 'status') -eq 'Blocked' }).Count
    $roleQuestions = @($Items | Where-Object { $_.Kind -eq 'question' -and (Get-Field $_ 'status') -eq 'Open' -and (Get-Field $_ 'to') -ne 'user' }).Count
    if ($active -eq 0 -and $roleQuestions -eq 0 -and $blocked -gt 0) {
        Add-Problem 'WARNING' 'tasks' ("no agent has open work while {0} tasks are blocked: follow the waits and break the chain, or ask the user" -f $blocked)
    }
}

# Every number from 1 to the highest must have a file, be in archive/done/, or be listed in
# archive/done/removed.md. A gap is a file that was lost or never written.
function Test-Gaps($Items) {
    $known = Get-GoneOnPurpose
    foreach ($it in $Items) { $known[$it.File] = $true }
    # Issued ids (ids.log) count too: an id that was issued and has no file was lost.
    $issued = Get-IssuedIds
    $script:missingIds = New-Object 'System.Collections.Generic.List[string]'
    foreach ($kind in $kinds.Keys) {
        $prefix = $kinds[$kind].Prefix
        $numbers = @(@($known.Keys) + @($issued.Keys) | Where-Object { $_ -match ('^' + $prefix + '-\d+$') } | ForEach-Object { Get-IdNumber $_ })
        if ($numbers.Count -eq 0) { continue }
        $max = ($numbers | Measure-Object -Maximum).Maximum
        $missing = New-Object 'System.Collections.Generic.List[string]'
        for ($n = 1; $n -le $max; $n++) { $id = Format-Id $prefix $n; if (-not $known.ContainsKey($id)) { $missing.Add($id); $script:missingIds.Add($id) } }
        if ($missing.Count -eq 0) { continue }
        $named = @($missing | ForEach-Object { if ($issued.Contains($_) -and $issued[$_].Title) { '{0} ("{1}")' -f $_, ($issued[$_].Title -replace '^(.{50}).+$', '$1...') } else { $_ } })
        Add-Problem 'ERROR' $kinds[$kind].Folder ('{0} missing: no file and not in archive/done/. Run -Restore missing: it restores from the newest checked copy, or recreates the item under the same id from ids.log. If removed on purpose, list it in archive/done/removed.md with the reason' -f ($named -join ', '))
        $script:orchestratorWork.Add('restore ' + ($missing -join ', ') + ' (-Restore missing)')
        $script:somethingLost = $true
    }
}

# "Skill version: 2026-11-09.1" in RULES.md and in every agent definition; '' when a file has none.
function Get-SkillVersion([string]$Path) {
    foreach ($l in (Read-Lines $Path)) { if ($l -match 'Skill version:\**\s*([0-9][0-9A-Za-z.-]*[0-9A-Za-z])') { return $Matches[1] } }
    return ''
}

# The skill version in which the rules about the product (requirements, architecture, security,
# tests) last changed, from "Product rules last changed: <version>" in RULES.md; '' when absent.
function Get-ProductRulesVersion([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    foreach ($l in (Read-Lines $Path)) { if ($l -match 'Product rules last changed:\**\s*([0-9][0-9A-Za-z.-]*[0-9A-Za-z])') { return $Matches[1] } }
    return ''
}

# Compares two skill versions (2026-11-20.9 against 2026-11-20.10) part by part, as numbers:
# -1, 0 or 1.
function Compare-SkillVersion([string]$A, [string]$B) {
    $pa = @($A -split '[.-]'); $pb = @($B -split '[.-]')
    for ($i = 0; $i -lt [Math]::Max($pa.Count, $pb.Count); $i++) {
        $x = 0; $y = 0
        if ($i -lt $pa.Count) { [void][int]::TryParse($pa[$i], [ref]$x) }
        if ($i -lt $pb.Count) { [void][int]::TryParse($pb[$i], [ref]$y) }
        if ($x -lt $y) { return -1 }
        if ($x -gt $y) { return 1 }
    }
    return 0
}

# The value of the last "Tool" row under "AI environment" in project.md; '' when there is none.
function Get-ToolLine {
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (-not (Test-Path -LiteralPath $projectFile)) { return '' }
    $toolLine = @(Read-Lines $projectFile | Where-Object { $_ -match '^\|\s*Tool\b' } | Select-Object -Last 1) -join ''
    # Only the value cell: the label gives examples of every tool.
    $cells = @(Get-Cells $toolLine)
    if ($cells.Count -ge 2) { return $cells[1] }
    return ''
}

# Where the agent definitions of the team's tool live, in the repository and at user level:
# GitHub Copilot reads .github/agents, Claude Code reads .claude/agents. Both when the tool is unknown.
function Get-AgentPlaces([string]$ToolLine) {
    $root = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Parent
    $useCopilot = $ToolLine -match '(?i)Copilot|Visual Studio|VS Code'
    $useClaude = $ToolLine -match '(?i)Claude Code|Claude app|Claude desktop'
    if (-not $useCopilot -and -not $useClaude) { $useCopilot = $true; $useClaude = $true }
    $places = New-Object 'System.Collections.Generic.List[object]'
    if ($useCopilot) { $places.Add(@{ Label = '.github/agents'; Path = (Join-Path $root '.github\agents'); Filter = '*.agent.md' }) }
    if ($useClaude) { $places.Add(@{ Label = '.claude/agents'; Path = (Join-Path $root '.claude\agents'); Filter = '*.md' }) }
    if ($env:USERPROFILE) {
        if ($useCopilot) { $places.Add(@{ Label = '~/.github/agents'; Path = (Join-Path $env:USERPROFILE '.github\agents'); Filter = '*.agent.md' }) }
        if ($useClaude) { $places.Add(@{ Label = '~/.claude/agents'; Path = (Join-Path $env:USERPROFILE '.claude\agents'); Filter = '*.md' }) }
    }
    return $places
}

# Copies $Source over $Target when their contents differ; $true when it copied.
function Copy-IfChanged([string]$Source, [string]$Target) {
    if ((Test-Path -LiteralPath $Target) -and (Get-FileHash -LiteralPath $Source).Hash -eq (Get-FileHash -LiteralPath $Target).Hash) { return $false }
    New-Item -ItemType Directory -Path (Split-Path $Target -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Target -Force
    return $true
}

# Updates the team from the skill folder in one step (skill update, step 1): RULES.md, HELP.md,
# this script, the solution-type and prompt files the team has, and the text of every agent
# definition of the team's tool. An agent definition keeps its own header (the environment's
# name, description, model and tools); only the text after it comes from the skill. Only files
# that differ are written, so a second run changes nothing.
function Invoke-SkillUpdate([string]$SkillFolder) {
    $SkillFolder = $SkillFolder.Trim().Trim('"').Trim("'")
    if (-not (Test-Path -LiteralPath (Join-Path $SkillFolder 'templates\RULES.md')) -or -not (Test-Path -LiteralPath (Join-Path $SkillFolder 'agents'))) {
        Write-Output "$SkillFolder is not the development-team skill folder: it lacks templates/RULES.md or agents/"
        $script:exitCode = 1
        return
    }
    $root = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Parent
    # Before RULES.md is replaced: the product rules the team worked with (skill update, step 3).
    $teamRules = Join-Path $TeamFolder 'RULES.md'
    $teamProduct = ''
    if (Test-Path -LiteralPath $teamRules) { $teamProduct = Get-ProductRulesVersion $teamRules; if (-not $teamProduct) { $teamProduct = Get-SkillVersion $teamRules } }
    $skillProduct = Get-ProductRulesVersion (Join-Path $SkillFolder 'templates\RULES.md')
    $updated = New-Object 'System.Collections.Generic.List[string]'
    $copies = New-Object 'System.Collections.Generic.List[object]'
    $copies.Add(@{ From = 'templates\RULES.md'; To = (Join-Path $TeamFolder 'RULES.md'); Label = 'RULES.md' })
    $copies.Add(@{ From = 'templates\HELP.md'; To = (Join-Path $TeamFolder 'HELP.md'); Label = 'HELP.md' })
    $copies.Add(@{ From = 'scripts\check-decisions.ps1'; To = (Join-Path $TeamFolder 'check-decisions.ps1'); Label = 'check-decisions.ps1' })
    # Solution-type files and prompt files: only the ones the team already has.
    $typesDir = Join-Path $TeamFolder 'solution-types'
    if (Test-Path -LiteralPath $typesDir) { foreach ($f in @(Get-ChildItem -LiteralPath $typesDir -Filter '*.md' -File)) { $copies.Add(@{ From = "solution-types\$($f.Name)"; To = $f.FullName; Label = "solution-types/$($f.Name)" }) } }
    $promptsDir = Join-Path $root '.github\prompts'
    # When the team uses the prompt files, all of the skill's: also commands that are new.
    if ((Test-Path -LiteralPath $promptsDir) -and @(Get-ChildItem -LiteralPath $promptsDir -Filter 'team-*.prompt.md' -File).Count -gt 0) {
        foreach ($f in @(Get-ChildItem -LiteralPath (Join-Path $SkillFolder 'prompts') -Filter 'team-*.prompt.md' -File)) { $copies.Add(@{ From = "prompts\$($f.Name)"; To = (Join-Path $promptsDir $f.Name); Label = ".github/prompts/$($f.Name)" }) }
    }
    foreach ($c in $copies) {
        $src = Join-Path $SkillFolder $c.From
        if (-not (Test-Path -LiteralPath $src)) { Write-Output "$($c.Label) is not in the skill: left as it is"; continue }
        if (Copy-IfChanged $src $c.To) { $updated.Add($c.Label) }
    }
    # Agent definitions: the file's own header, then the skill's text after its header.
    $header = '(?s)^---\r?\n.*?\r?\n---\r?\n'
    foreach ($p in @(Get-AgentPlaces (Get-ToolLine))) {
        if (-not (Test-Path -LiteralPath $p.Path)) { continue }
        foreach ($f in @(Get-ChildItem -LiteralPath $p.Path -Filter $p.Filter -File)) {
            $type = $f.Name -replace '\.agent\.md$', '' -replace '\.md$', ''
            $src = Join-Path (Join-Path $SkillFolder 'agents') "$type.md"
            if ($agentTypes -notcontains $type -or -not (Test-Path -LiteralPath $src)) { continue }
            $cur = [System.IO.File]::ReadAllText($f.FullName)
            $m = [regex]::Match($cur, $header)
            if (-not $m.Success) { Write-Output "$($p.Label)/$($f.Name) has no header between two '---' lines: not updated, regenerate it by hand"; continue }
            $body = [regex]::Replace([System.IO.File]::ReadAllText($src), $header, '') -replace "`r`n", "`n"
            if ($m.Value.Contains("`r`n")) { $body = $body -replace "`n", "`r`n" }
            $new = $m.Value + $body
            if ($new -cne $cur) { Write-TextFile $f.FullName $new; $updated.Add("$($p.Label)/$($f.Name)") }
        }
    }
    # A mandatory role that is new in the skill: in every place that has the team's agent
    # definitions, create its definition with the header of a role of the same kind (so it keeps
    # this environment's tool names and model) and the skill's text; add it to the Team table.
    $newRoles = [ordered]@{ 'code-reviewer' = @{ Like = 'security-officer'; Owns = 'Code reviews in detail (reviews/code-reviews.md)' } }
    foreach ($p in @(Get-AgentPlaces (Get-ToolLine))) {
        if (-not (Test-Path -LiteralPath $p.Path)) { continue }
        $ext = '.md'; if ($p.Filter -eq '*.agent.md') { $ext = '.agent.md' }
        foreach ($type in @($newRoles.Keys)) {
            $target = Join-Path $p.Path "$type$ext"
            $like = Join-Path $p.Path "$($newRoles[$type].Like)$ext"
            $src = Join-Path (Join-Path $SkillFolder 'agents') "$type.md"
            if ((Test-Path -LiteralPath $target) -or -not (Test-Path -LiteralPath $like) -or -not (Test-Path -LiteralPath $src)) { continue }
            $m = [regex]::Match([System.IO.File]::ReadAllText($like), $header)
            if (-not $m.Success) { Write-Output "$($p.Label)/$($newRoles[$type].Like)$ext has no header: create $type$ext by hand"; continue }
            $skillText = [System.IO.File]::ReadAllText($src)
            $desc = ''
            if ($skillText -match '(?m)^description:[ \t]*([^\r\n]+)') { $desc = $Matches[1].Trim().Trim('"') -replace '"', "'" }
            $head = [regex]::Replace($m.Value, '(?m)^name:[^\r\n]*', "name: $type")
            $head = [regex]::Replace($head, '(?m)^description:[^\r\n]*', { 'description: "' + $desc + '"' })
            $body = [regex]::Replace($skillText, $header, '') -replace "`r`n", "`n"
            if ($m.Value.Contains("`r`n")) { $body = $body -replace "`n", "`r`n" }
            Write-TextFile $target ($head + $body)
            $updated.Add("$($p.Label)/$type$ext (new: header of $($newRoles[$type].Like), check its tools)")
        }
    }
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (Test-Path -LiteralPath $projectFile) {
        $text = [System.IO.File]::ReadAllText($projectFile)
        $before = $text
        $nl = "`n"; if ($text.Contains("`r`n")) { $nl = "`r`n" }
        foreach ($type in @($newRoles.Keys)) {
            if ($text -match ('(?m)^\|\s*' + [regex]::Escape($type) + '\s*\|')) { continue }
            $row = [regex]::Match($text, '(?m)^\|\s*' + [regex]::Escape($newRoles[$type].Like) + '\s*\|[^\r\n]*')
            if (-not $row.Success) { continue }
            $cells = @(Get-Cells $row.Value)
            if ($cells.Count -lt 3) { continue }
            $cells[0] = $type; $cells[1] = $type; $cells[$cells.Count - 1] = $newRoles[$type].Owns
            $text = $text.Insert($row.Index + $row.Length, $nl + '| ' + ($cells -join ' | ') + ' |')
            $updated.Add("project.md (Team table: $type)")
        }
        if ($text -cne $before) { Write-TextFile $projectFile $text }
    }
    # Review templates the team does not have yet (for example reviews/code-reviews.md).
    $templateReviews = Join-Path (Join-Path $SkillFolder 'templates') 'reviews'
    if (Test-Path -LiteralPath $templateReviews) {
        foreach ($f in @(Get-ChildItem -LiteralPath $templateReviews -Filter '*.md' -File)) {
            $t = Join-Path (Join-Path $TeamFolder 'reviews') $f.Name
            if (Test-Path -LiteralPath $t) { continue }
            New-Item -ItemType Directory -Path (Split-Path $t -Parent) -Force | Out-Null
            Copy-Item -LiteralPath $f.FullName -Destination $t
            $updated.Add("reviews/$($f.Name) (new, from the template)")
        }
    }
    if ($updated.Count -eq 0) { Write-Output "Already up to date with the skill in $SkillFolder" }
    foreach ($u in $updated) { Write-Output "Updated $u" }
    $next = 'Set "Last checked against the skill" in project.md'
    if (@($updated | Where-Object { $_ -match '^\.(github|claude)/' }).Count -gt 0) { $next += ', and commit the changed repository files in a commit of their own (SKILL.md, "Version control and pull requests", step 6)' }
    Write-Output ('Skill version now: {0}. {1}.' -f (Get-SkillVersion (Join-Path $TeamFolder 'RULES.md')), $next)
    if ($skillProduct -and $teamProduct) {
        if ((Compare-SkillVersion $teamProduct $skillProduct) -lt 0) { Write-Output ('Product rules changed: the team worked with {0}, the skill has {1}. Create the re-check tasks of the skill update, step 3.' -f $teamProduct, $skillProduct) }
        else { Write-Output ('Product rules unchanged since {0}: no re-check tasks needed (skill update, step 3).' -f $skillProduct) }
    }
    if ($updated -contains 'check-decisions.ps1') { Write-Output 'The script itself changed: the check below still runs the old one, so run -Build once more.' }
}

# Team files outside the team folder, and agent definitions from another skill version.
function Test-Environment {
    $rules = Join-Path $TeamFolder 'RULES.md'
    if (-not (Test-Path -LiteralPath $rules)) { Add-Problem 'ERROR' 'RULES.md' 'is missing: copy it from the skill (templates/RULES.md)'; return }
    $root = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Parent
    foreach ($name in @('decisions.md', 'RULES.md', 'HELP.md', 'project.md', 'check-decisions.ps1')) {
        if (Test-Path -LiteralPath (Join-Path $root $name)) {
            Add-Problem 'WARNING' $name 'is in the repository root, outside the team folder: agents may read it instead of the team''s own file. Remove it (move anything new in it into the team folder first)'
            $script:orchestratorWork.Add("remove $name from the repository root")
        }
    }
    # Files the agents create stay in the repository (RULES.md, rule 16): scratch files and
    # command output go to .tmp/ in its root, which git must ignore.
    $ignoreFile = Join-Path $root '.gitignore'
    if (-not (Test-Path -LiteralPath $ignoreFile) -or -not (@(Read-Lines $ignoreFile | Where-Object { $_.Trim() -match '^/?\.tmp/?$' }).Count)) {
        Add-Problem 'WARNING' '.gitignore' 'does not ignore .tmp/, where agents put scratch files and command output (RULES.md, rule 16): add the line .tmp/'
        $script:orchestratorWork.Add('add .tmp/ to .gitignore (in a commit of its own when project.md allows commits)')
    }
    # A Visual Studio solution must list every top-level team file in its .team folder: agents
    # (for example GitHub Copilot) often cannot search a git-ignored team folder otherwise.
    $teamName = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Leaf
    $wanted = @(@('RULES.md', 'HELP.md', 'decisions.md', 'report.md', 'standup.md') + @(Get-DocumentNames) | Where-Object { Test-Path -LiteralPath (Join-Path $TeamFolder $_) } | Select-Object -Unique)
    foreach ($sln in @(Get-ChildItem -LiteralPath $root -File | Where-Object { $_.Extension -in @('.sln', '.slnx') })) {
        $text = [System.IO.File]::ReadAllText($sln.FullName) -replace '\\', '/'
        $absent = @($wanted | Where-Object { $text.IndexOf("$teamName/$_", [System.StringComparison]::OrdinalIgnoreCase) -lt 0 })
        if ($absent.Count -eq 0) { continue }
        Add-Problem 'WARNING' $sln.Name ('lacks these team files in its {0} solution folder: {1}. Add them, so they show in Solution Explorer and agents'' searches find them' -f $teamName, ($absent -join ', '))
        $script:orchestratorWork.Add("add $($absent -join ', ') to the $teamName folder of $($sln.Name)")
    }

    $projectFile = Join-Path $TeamFolder 'project.md'
    if ((Test-Path -LiteralPath $projectFile) -and @(Read-Lines $projectFile | Where-Object { $_ -match '^\*\*Agents at the same time:\*\*\s*(yes|no)\b' }).Count -eq 0) {
        Add-Problem 'WARNING' 'project.md' 'does not say "Agents at the same time: yes" or "no": ask the user whether the agents run one at a time, and record it (see the project.md template)'
        $script:orchestratorWork.Add('ask the user whether agents run one at a time, and record "Agents at the same time" in project.md')
    }
    $version = Get-SkillVersion $rules
    if (-not $version) {
        Add-Problem 'WARNING' 'RULES.md' 'has no skill version: it is older than the skill. Update the rules files and the agent definitions (skill update, step 1)'
        $script:orchestratorWork.Add('update the rules files and agent definitions (skill update, step 1)')
        return
    }
    # Repository and user-level agent definitions, only of the tool the team uses ("AI environment"
    # in project.md): GitHub Copilot reads .github/agents, Claude Code reads .claude/agents.
    $toolRows = @(Read-Lines $projectFile | Where-Object { $_ -match '^\|\s*Tool\b' })
    if ($toolRows.Count -gt 1) {
        Add-Problem 'WARNING' 'project.md' ('lists {0} tools under "AI environment": keep one row, with the tool the team uses now (the check uses the last one)' -f $toolRows.Count)
        $script:orchestratorWork.Add('keep one Tool row under AI environment in project.md')
    }
    $toolLine = Get-ToolLine
    $places = @(Get-AgentPlaces $toolLine)
    $types = $agentTypes
    # Tool names that belong to one environment only, to catch definitions left from another tool.
    $foreign = $null
    if ($toolLine -match '(?i)VS Code|Visual Studio Code') { $foreign = @{ Env = 'VS Code'; Names = @('get_file', 'run_command_in_terminal', 'get_web_pages', 'replace_string_in_file', 'multi_replace_string_in_file') } }
    elseif ($toolLine -match '(?i)Visual Studio') { $foreign = @{ Env = 'Visual Studio'; Names = @('read_file', 'run_in_terminal', 'fetch_webpage', 'apply_patch', 'semantic_search') } }
    $broken = $false
    $outdated = $false
    foreach ($p in $places) {
        if (-not (Test-Path -LiteralPath $p.Path)) { continue }
        $old = New-Object 'System.Collections.Generic.List[string]'
        $found = New-Object 'System.Collections.Generic.List[string]'
        $present = New-Object 'System.Collections.Generic.List[string]'
        foreach ($f in @(Get-ChildItem -LiteralPath $p.Path -Filter $p.Filter -File)) {
            $type = $f.Name -replace '\.agent\.md$', '' -replace '\.md$', ''
            if ($types -notcontains $type) { continue }
            $present.Add($type)
            # The header between the first two '---' lines: every key once, and the tools of this environment.
            $lines = @(Read-Lines $f.FullName)
            $start = -1; $end = -1
            for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim([char]0xFEFF, [char]32) -eq '---') { if ($start -lt 0) { $start = $i } else { $end = $i; break } } }
            if ($start -lt 0 -or $end -lt 0) {
                Add-Problem 'WARNING' "$($p.Label)/$($f.Name)" "has no header between two '---' lines: regenerate it"
                $broken = $true
            } else {
                $keys = @{}
                $header = @($lines[($start + 1)..($end - 1)])
                foreach ($h in $header) { if ($h -match '^([A-Za-z][\w-]*)\s*:') { $keys[$Matches[1]] = 1 + [int]$keys[$Matches[1]] } }
                $twice = @($keys.Keys | Where-Object { $keys[$_] -gt 1 } | Sort-Object)
                if ($twice.Count -gt 0) {
                    Add-Problem 'WARNING' "$($p.Label)/$($f.Name)" ("has {0} more than once in its header: the tool may reject it or take the wrong line. Keep one" -f ($twice -join ', '))
                    $broken = $true
                }
                if ($foreign -and $p.Label -like '*.github/agents') {
                    $toolsText = (@($header | Where-Object { $_ -match '^tools\s*:' }) -join ' ')
                    $wrong = @($foreign.Names | Where-Object { $toolsText -match ('\b' + $_ + '\b') })
                    if ($wrong.Count -gt 0) {
                        Add-Problem 'WARNING' "$($p.Label)/$($f.Name)" ("names tools of another environment ({0}) while project.md says {1}: regenerate it with {1}'s tool names" -f ($wrong -join ', '), $foreign.Env)
                        $broken = $true
                    }
                }
            }
            $v = Get-SkillVersion $f.FullName
            if ($v -ne $version) {
                $old.Add($type)
                if (-not $v) { $v = 'none' }
                if (-not $found.Contains($v)) { $found.Add($v) }
            }
        }
        if ($old.Count -gt 0) {
            Add-Problem 'WARNING' $p.Label ('{0} agent definitions have skill version {1}, RULES.md has {2}: regenerate them from the skill (skill update, step 1). Roles started with them follow old instructions' -f $old.Count, ($found -join ' or '), $version)
            $outdated = $true
        }
        # The team reporter answers "team report" in its own chat while the team works.
        if ($present.Count -gt 0 -and -not $present.Contains('team-reporter')) {
            Add-Problem 'WARNING' $p.Label 'has the team''s agent definitions but no team-reporter: generate it from the skill''s agents/team-reporter.md, so /team-report answers in its own chat while the team works'
            $outdated = $true
        }
        # The code reviewer is mandatory since skill 2026-11-20.8: every pull request is reviewed.
        if ($present.Count -gt 0 -and -not $present.Contains('code-reviewer')) {
            Add-Problem 'WARNING' $p.Label 'has the team''s agent definitions but no code-reviewer: generate it from the skill''s agents/code-reviewer.md (the same header fields as the other roles, the reasoning tier), add it to the Team table in project.md and copy templates/reviews/code-reviews.md to reviews/'
            $outdated = $true
        }
    }
    if ($outdated) { $script:orchestratorWork.Add('regenerate the agent definitions') }
    elseif ($broken) { $script:orchestratorWork.Add('repair the agent definitions (see the warnings)') }
}

function Find-Cycle([string]$Node, [System.Collections.Generic.List[string]]$Path) {
    if ($script:visiting.ContainsKey($Node)) {
        $loop = @($Path.GetRange($Path.IndexOf($Node), $Path.Count - $Path.IndexOf($Node)))
        $lowest = ($loop | Sort-Object | Select-Object -First 1)
        $at = [array]::IndexOf($loop, $lowest)
        $ordered = @($loop[$at..($loop.Count - 1)]) + @(if ($at -gt 0) { $loop[0..($at - 1)] }) + $lowest
        $text = $ordered -join ' -> '
        if (-not $script:cycles.Contains($text)) { $script:cycles.Add($text) }
        return
    }
    if ($script:visited.ContainsKey($Node)) { return }
    $script:visiting[$Node] = $true
    $Path.Add($Node)
    if ($script:edges.ContainsKey($Node)) { foreach ($next in $script:edges[$Node]) { Find-Cycle $next $Path } }
    $Path.RemoveAt($Path.Count - 1)
    $script:visiting.Remove($Node)
    $script:visited[$Node] = $true
}

function Test-Documents([string]$BaselineFolder) {
    if (-not (Test-Path -LiteralPath (Join-Path $TeamFolder 'project.md'))) { Add-Problem 'ERROR' 'project.md' 'is missing: run -Restore missing'; $script:orchestratorWork.Add('restore project.md (-Restore missing)') }
    # A document in the checked copy that is gone now was lost.
    foreach ($d in (Get-MissingDocuments $BaselineFolder)) {
        if ($d -eq 'project.md') { continue }
        Add-Problem 'ERROR' $d 'is in the -ok copy but missing now: run -Restore missing'
        $script:orchestratorWork.Add("restore $d (-Restore missing)")
        $script:somethingLost = $true
    }
    foreach ($d in (Get-DocumentNames)) {
        $path = Join-Path $TeamFolder $d
        $now = @(Read-Lines $path | Where-Object { $_.Trim() }).Count
        if ($now -eq 0) { Add-Problem 'ERROR' $d 'is empty: run -Restore missing'; $script:orchestratorWork.Add("restore $d (-Restore missing)"); continue }
        if ($BaselineFolder) {
            $old = Join-Path $BaselineFolder $d
            if (Test-Path -LiteralPath $old) {
                # A document that moved details into its folder (architecture/) is compared with
                # its folder included, so a split is not reported as a cut.
                $whole = $documents -contains $d
                $before = Get-TextLineCount $BaselineFolder $d $whole
                $after = Get-TextLineCount $TeamFolder $d $whole
                $folder = [System.IO.Path]::GetFileNameWithoutExtension($d)
                $with = ''
                if ($whole -and (Test-Path -LiteralPath (Join-Path $TeamFolder $folder))) { $with = " (with $folder/)" }
                if ($before -ge 20 -and $after -lt ($before / 2)) { Add-Problem 'ERROR' $d ("shrank from {0} to {1} lines with text{2} since the -ok copy: check it was not cut" -f $before, $after, $with) }
            }
        }
    }
}

# A Proposed decision must lead somewhere: an open question (usually to the user, for the OK)
# or an unfinished task must name it. Otherwise it is forgotten and nothing happens.
function Test-Decisions($Items) {
    $text = New-Object 'System.Text.StringBuilder'
    foreach ($it in @($Items | Where-Object { $_.HasHeader })) {
        $status = Get-Field $it 'status'
        if (($it.Kind -eq 'question' -and $status -eq 'Open') -or ($it.Kind -eq 'task' -and $status -ne 'Done')) {
            [void]$text.AppendLine((Get-Field $it 'title')).AppendLine((Get-Field $it 'waits-for')).AppendLine($it.Body)
        }
    }
    $all = $text.ToString()
    $named = @{}
    foreach ($m in [regex]::Matches($all, '(?i)\bD-(\d+)\b(?:\s*(?:to|-|\.\.|\u2013)\s*D-(\d+)\b)?')) {
        $from = [int]$m.Groups[1].Value; $to = $from
        if ($m.Groups[2].Success) { $to = [int]$m.Groups[2].Value }
        for ($n = $from; $n -le $to -and $n -le $from + 200; $n++) { $named[(Format-Id 'D' $n)] = $true }
    }
    $forgotten = @($Items | Where-Object { $_.Kind -eq 'decision' -and $_.HasHeader -and (Get-Field $_ 'status') -eq 'Proposed' -and -not $named.ContainsKey($_.File) } |
            Sort-Object { Get-IdNumber $_.File } | ForEach-Object { $_.File })
    if ($forgotten.Count -eq 0) { return }
    Add-Problem 'WARNING' 'decisions' ('{0} Proposed, and no open question or unfinished task names it: nothing happens with it. Put it to the user as a question (or to its reviewer as a task), or reject it' -f ($forgotten -join ', '))
    $script:orchestratorWork.Add('follow up ' + ($forgotten -join ', ') + ' (a question to the user or a task)')
}

# Lines of a git command in the repository; none when git is missing or fails (never stops the check).
function Get-GitLines([string]$RepoRoot, [string[]]$GitArgs) {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { return @() }
    $lines = @()
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $lines = @(& git -C $RepoRoot @GitArgs 2>$null); if ($LASTEXITCODE -ne 0) { $lines = @() } } catch { $lines = @() } finally { $ErrorActionPreference = $previous }
    return $lines
}

# The user approves an item in steps: after every finished task, the item's open pull request is
# ready for review (when project.md allows pull requests by the orchestrator). A Done task without
# 'merged:' is not recorded as merged. When its commits (subject 'T-033 ...') are on the base
# branch and none is left on the item's branch, it is merged: the orchestrator records it.
# Otherwise: without 'pr:' on its item (and with a branch: the item's 'branch:', or a git branch
# whose name contains the item id), it needs a pull request; with a draft 'pr:', the pull request
# must be taken out of draft; with a ready 'pr:' link, the user is asked to approve it.
# 'pr: none (reason)' is an exception on purpose; 'merged:' on the item means all of it is merged.
function Test-PullRequests($Items) {
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (-not (Test-Path -LiteralPath $projectFile)) { return }
    $projectLines = @(Read-Lines $projectFile)
    if (@($projectLines | Where-Object { $_ -match '^\|\s*Pull requests by the orchestrator[^|]*\|\s*yes\b' }).Count -eq 0) { return }
    $repoRoot = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Parent
    $branches = @(Get-GitLines $repoRoot @('for-each-ref', '--format=%(refname:short)', 'refs/heads', 'refs/remotes'))
    $base = 'main'
    foreach ($l in $projectLines) {
        if ($l -match '^\|\s*Base branch\s*\|\s*([^|{]+?)\s*\|') { $b = @($Matches[1] -split '[\s;,(]')[0]; if ($b) { $base = $b }; break }
    }
    $baseRef = ''
    if ($branches -contains "origin/$base") { $baseRef = "origin/$base" } elseif ($branches -contains $base) { $baseRef = $base }
    $onBase = @{}
    if ($baseRef) { foreach ($s in @(Get-GitLines $repoRoot @('log', $baseRef, '--format=%s'))) { if ($s -match '^(T-\d+)\b') { $onBase[$Matches[1]] = $true } } }
    $today = Get-Date -Format 'yyyy-MM-dd'
    $forTasks = @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'for') -match '^(REQ|D)-\d+$' })
    $done = @($forTasks | Where-Object { (Get-Field $_ 'status') -eq 'Done' -and -not (Get-Field $_ 'merged') })
    foreach ($id in @($done | ForEach-Object { Get-Field $_ 'for' } | Sort-Object -Unique)) {
        $item = @($Items | Where-Object { $_.File -eq $id -and $_.HasHeader })
        if ($item.Count -eq 0 -or (Get-Field $item[0] 'merged')) { continue }
        $where = Get-RelativePath $item[0].Path
        $tasks = @($done | Where-Object { (Get-Field $_ 'for') -eq $id } | ForEach-Object { Get-Field $_ 'id' } | Sort-Object)
        $branch = Get-Field $item[0] 'branch'
        $recordBranch = ''
        if (-not $branch) {
            $hit = @($branches | Where-Object { $_ -match ('(^|/)' + [regex]::Escape($id) + '($|[-_/])') } | Sort-Object Length | Select-Object -First 1)
            if ($hit.Count -gt 0) { $branch = $hit[0] -replace '^origin/', ''; $recordBranch = (' and branch: {0}' -f $branch) }
        }
        # Merged on the base branch, not recorded yet.
        if ($branch -and $baseRef) {
            $ref = ''
            # Commits not on the base branch yet, on the pushed and on the local branch.
            $pending = @{}
            foreach ($ref in @("origin/$branch", $branch)) {
                if ($branches -notcontains $ref) { continue }
                foreach ($s in @(Get-GitLines $repoRoot @('log', "$baseRef..$ref", '--format=%s'))) { if ($s -match '^(T-\d+)\b') { $pending[$Matches[1]] = $true } } }
            # A Done task without any commit (for example "nothing to change") needs no pull request.
            $tasks = @($tasks | Where-Object { $onBase.ContainsKey($_) -or $pending.ContainsKey($_) })
            if ($tasks.Count -eq 0) { continue }
            $merged = @($tasks | Where-Object { $onBase.ContainsKey($_) -and -not $pending.ContainsKey($_) })
            if ($merged.Count -gt 0) {
                Add-Problem 'WARNING' $where ('{0} merged into {1}, but not recorded: write merged: {2} in their headers, move the pull request link from pr: to merged-prs: in {3}, and tell the user' -f ($merged -join ', '), $base, $today, $id)
                $script:orchestratorWork.Add(('record the merge of {0}: merged: {1} in {2}, its link from pr: to merged-prs:' -f $id, $today, ($merged -join ', ')))
                $tasks = @($tasks | Where-Object { $merged -notcontains $_ })
                # The pr: link is the merged pull request: the other Done tasks need a new one.
                if ($tasks.Count -gt 0) { $script:orchestratorWork.Add(('then open a new pull request for {0} ({1}) from the same branch and ask the user to approve it' -f $id, ($tasks -join ', '))) }
                continue
            }
        }
        $pr = Get-Field $item[0] 'pr'
        if ($pr) {
            if ($pr -notmatch '(https?://[^\s)\]>]+)') { continue }
            $link = $Matches[1]
            if ($pr -match '(?i)\bdraft\b') {
                Add-Problem 'WARNING' $where ('{0} Done and not merged, but the pull request of {1} is still a draft: the GitHub Pull Requests extension cannot change that, so ask the user to press Ready for review on GitHub, then remove (draft) from pr: and ask the user to approve it' -f ($tasks -join ', '), $id)
                $script:orchestratorWork.Add(('get the pull request of {0} out of draft (the user presses Ready for review), remove (draft) from its pr: line and ask the user to approve it' -f $id))
            }
            else {
                # First the code reviewer and the architect review the pull request; when both
                # approve, the orchestrator records '(reviewed 16:58 for T-036, T-056' after the
                # link in pr:. Then it asks the user to approve and adds '; approval asked 17:05
                # for T-036, T-056)'. A task that joined since is reviewed and asked again; a task
                # the user was already asked about needs no review first.
                $asked = ''
                if ($pr -match '(?i)approval asked([^)]*)') { $asked = $Matches[1] }
                $reviewed = ''
                if ($pr -match '(?i)\breviewed([^;)]*)') { $reviewed = $Matches[1] }
                $notAsked = @($tasks | Where-Object { $asked -notmatch ('\b' + [regex]::Escape($_) + '\b') })
                $notReviewed = @($notAsked | Where-Object { $reviewed -notmatch ('\b' + [regex]::Escape($_) + '\b') })
                $reviewers = 'the code reviewer (the code in detail) and the architect (the change as a whole; not needed when the pull request changes only tests)'
                if ($notReviewed.Count -gt 0) {
                    $now = Get-Date -Format 'HH:mm'
                    Add-Problem 'WARNING' $where ('the pull request of {0} is ready, but {1} is not reviewed yet: have {2} review it, in their task for {0} (-Reopen it when they have one); when the latest verdicts in reviews/code-reviews.md are Approve, write (reviewed {3} up to <commit> for {4}) after the link in pr: and then ask the user to approve' -f $id, ($notReviewed -join ', '), $reviewers, $now, ($tasks -join ', '))
                    $script:orchestratorWork.Add(('have the pull request of {0} ({1}) reviewed by {2}, fix what they find Blocking, then write (reviewed {3} up to <commit> for {1}) after the link {4} in its pr: line and ask the user to approve' -f $id, ($tasks -join ', '), $reviewers, $now, $link))
                    continue
                }
                # Reviewed up to a commit: changes after it need a new review, except whitespace
                # (indentation, blank lines), which git diff -w --ignore-blank-lines leaves out.
                if ($branch -and $reviewed -match '(?i)\bup to\s+([0-9a-f]{7,40})\b') {
                    $sha = $Matches[1]
                    $tipRef = @(@("origin/$branch", $branch) | Where-Object { $branches -contains $_ } | Select-Object -First 1)
                    if ($tipRef.Count -gt 0 -and @(Get-GitLines $repoRoot @('rev-parse', '--verify', '--quiet', "$sha^{commit}")).Count -gt 0) {
                        $changed = @(Get-GitLines $repoRoot @('diff', '-w', '--ignore-blank-lines', '--name-only', $sha, $tipRef[0]))
                        if ($changed.Count -gt 0) {
                            $now = Get-Date -Format 'HH:mm'
                            Add-Problem 'WARNING' $where ('the pull request of {0} changed after the review up to {1} ({2} files, whitespace not counted): reopen the review tasks for the commits after {1} (-Reopen); when the latest verdicts are Approve again, replace the note after the link in pr: with (reviewed {3} up to <commit> for {4}) and ask the user to approve' -f $id, $sha, $changed.Count, $now, ($tasks -join ', '))
                            $script:orchestratorWork.Add(('have the commits after {0} in the pull request of {1} reviewed (-Reopen the review tasks), then replace the note in its pr: line with (reviewed {2} up to <commit> for {3}) and ask the user to approve' -f $sha, $id, $now, ($tasks -join ', ')))
                            continue
                        }
                    }
                }
                $script:readyPullRequests.Add(('{0} ({1}) {2}' -f $id, ($tasks -join ', '), $link))
                if ($notAsked.Count -gt 0) {
                    $now = Get-Date -Format 'HH:mm'
                    Add-Problem 'WARNING' $where ('the pull request of {0} is ready, but the user was not asked to approve {1}: ask now with your question tool, with its link, and write (approval asked {2} for {3}) after the link in pr:' -f $id, ($notAsked -join ', '), $now, ($tasks -join ', '))
                    $script:orchestratorWork.Add(('ask the user to approve the pull request of {0} ({1}) with your question tool, with its link {2}, then write (approval asked {3} for {1}) after the link in its pr: line' -f $id, ($tasks -join ', '), $link, $now))
                }
            }
            continue
        }
        if (-not $branch) { continue }
        Add-Problem 'WARNING' $where ('{0} Done on branch {1} and not merged, but {2} has no open pull request: open it with the tool in project.md, ready for review when the build is green and the tests pass (never as a draft), record pr: <link>{3} in its header and ask the user to approve it (pr: none (reason) when it needs none)' -f ($tasks -join ', '), $branch, $id, $recordBranch)
        $script:orchestratorWork.Add(('open the pull request for {0} (branch {1}, {2}), record pr:{3} in its header and ask the user to approve it' -f $id, $branch, ($tasks -join ', '), $recordBranch))
    }
}

# --- Scope: every task within the approved requirements, scope reviews, active time ----------

function Get-RequirementIds([string]$Text) {
    return @([regex]::Matches($Text, '\bREQ-\d+\b') | ForEach-Object { $_.Value } | Select-Object -Unique)
}

# The requirements a bug in testing.md (Bugs) names, directly or through the decisions it names;
# $null when the bug is not there.
function Get-BugRequirements([string]$BugId, $Items) {
    foreach ($r in @(Get-SectionRows (Join-Path $TeamFolder 'testing.md') 'Bugs')) {
        $line = ($r -join ' | ')
        if ($line -notmatch ('(^|\|)\s*' + [regex]::Escape($BugId) + '\s*(\||$)')) { continue }
        $reqs = New-Object 'System.Collections.Generic.List[string]'
        foreach ($id in @(Get-RequirementIds $line)) { $reqs.Add($id) }
        foreach ($m in [regex]::Matches($line, '\bD-\d+\b')) {
            $d = @($Items | Where-Object { $_.File -eq $m.Value -and $_.HasHeader })
            if ($d.Count -gt 0) { foreach ($id in @(Get-RequirementIds ((Get-Field $d[0] 'requirements') + ' ' + $d[0].Body))) { $reqs.Add($id) } }
        }
        return , @($reqs | Select-Object -Unique)
    }
    return $null
}

# Why work for this item is not within the approved requirements; '' when it is.
function Get-TraceProblem([string]$For, $Items) {
    if (-not $For) { return 'names no item (for:)' }
    if ($For -eq 'team') { return '' }
    $status = @{}
    foreach ($r in @($Items | Where-Object { $_.Kind -eq 'requirement' -and $_.HasHeader })) { $status[$r.File] = Get-Field $r 'status' }
    if ($For -match '^REQ-\d+$') {
        if (-not $status.ContainsKey($For)) { return "is for $For, which does not exist" }
        if ($status[$For] -eq 'Approved') { return '' }
        return ('is for {0}, which is {1}, not Approved' -f $For, $status[$For])
    }
    if ($For -match '^D-\d+$') {
        $d = @($Items | Where-Object { $_.File -eq $For -and $_.HasHeader })
        if ($d.Count -eq 0) { return "is for $For, which does not exist" }
        $reqs = @(Get-RequirementIds ((Get-Field $d[0] 'requirements') + ' ' + (Get-Field $d[0] 'title') + ' ' + $d[0].Body))
        if (@($reqs | Where-Object { $status[$_] -eq 'Approved' }).Count -gt 0) { return '' }
        return ('is for {0}, which names no approved requirement (requirements: in its header)' -f $For)
    }
    if ($For -match '^BUG-\d+$') {
        $reqs = Get-BugRequirements $For $Items
        if ($null -eq $reqs) { return "is for $For, which is not in testing.md (Bugs)" }
        if (@($reqs | Where-Object { $status[$_] -eq 'Approved' }).Count -gt 0) { return '' }
        return ('is for {0}, whose row in testing.md names no approved requirement' -f $For)
    }
    return "is for '$For', which is not a requirement, a decision, a bug or team"
}

# The team's active time, estimated from its activity: issued ids, commits and checked copies.
# Events less than 30 minutes apart count as work in between; a new stretch counts 10 minutes.
function Get-ActiveTime {
    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    $times = New-Object 'System.Collections.Generic.List[datetime]'
    $add = {
        param([string]$Text, [string]$Format)
        $dt = [datetime]::MinValue
        if ([datetime]::TryParseExact($Text, $Format, $culture, [System.Globalization.DateTimeStyles]::None, [ref]$dt)) { $times.Add($dt) }
    }
    foreach ($v in (Get-IssuedIds).Values) { & $add $v.Time 'yyyy-MM-dd HH:mm' }
    $start = [datetime]::MaxValue
    foreach ($t in $times) { if ($t -lt $start) { $start = $t } }
    $archive = Join-Path $TeamFolder 'archive'
    if (Test-Path -LiteralPath $archive) {
        foreach ($d in @(Get-ChildItem -LiteralPath $archive -Directory)) { if ($d.Name -match '^(\d{4}-\d{2}-\d{2}-\d{4})') { & $add $Matches[1] 'yyyy-MM-dd-HHmm' } }
    }
    $repoRoot = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Parent
    foreach ($l in @(Get-GitLines $repoRoot @('log', '--all', '--format=%ad', '--date=format-local:%Y-%m-%d %H:%M'))) { & $add ([string]$l).Trim() 'yyyy-MM-dd HH:mm' }
    # Only the team's own time: from its first issued id on.
    $sorted = @($times | Where-Object { $_ -ge $start } | Sort-Object -Unique)
    $perDay = [ordered]@{}
    $total = 0.0
    for ($i = 0; $i -lt $sorted.Count; $i++) {
        $day = $sorted[$i].ToString('yyyy-MM-dd')
        if (-not $perDay.Contains($day)) { $perDay[$day] = 0.0 }
        $minutes = 10.0
        if ($i -gt 0) { $gap = ($sorted[$i] - $sorted[$i - 1]).TotalMinutes; if ($gap -le 30) { $minutes = $gap } }
        $perDay[$day] += $minutes
        $total += $minutes
    }
    return [pscustomobject]@{ Hours = [Math]::Round($total / 60, 1); PerDay = $perDay }
}

function Test-Scope($Items) {
    $script:scope = [pscustomobject]@{ Outside = @(); Active = $null; BoundaryHours = 0; NewTasks = 0; NewDecisions = 0; NewRequirements = 0; ReviewedUpTo = '' }
    # 1. Every unfinished task, and every proposed decision, within the approved requirements.
    $outside = New-Object 'System.Collections.Generic.List[string]'
    foreach ($t in @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'status') -ne 'Done' })) {
        $p = Get-TraceProblem (Get-Field $t 'for') $Items
        if (-not $p) { continue }
        $outside.Add($t.File); $script:notNow[$t.File] = $true
        Add-Problem 'WARNING' (Get-RelativePath $t.Path) ('{0}: not within the approved requirements (RULES.md, rule 14). Link it to the requirement it serves, or ask the user whether it is needed; nobody works on it before' -f $p)
    }
    foreach ($d in @($Items | Where-Object { $_.Kind -eq 'decision' -and $_.HasHeader -and (Get-Field $_ 'status') -eq 'Proposed' })) {
        if ((Get-TraceProblem $d.File $Items) -eq '') { continue }
        $outside.Add($d.File)
        Add-Problem 'WARNING' (Get-RelativePath $d.Path) 'names no approved requirement it serves (requirements: in its header): within the requirements, or a question to the user first (RULES.md, rule 14)'
    }
    $script:scope.Outside = @($outside)
    if ($outside.Count -gt 0) { $script:orchestratorWork.Add(('check {0} against the requirements: link each to the approved requirement it serves, or ask the user whether it is needed (nobody works on them before)' -f ($outside -join ', '))) }

    # 2. A scope review by the sanity agent after 10 new tasks or decisions, or a new requirement.
    $review = Join-Path (Join-Path $TeamFolder 'reviews') 'sanity-reviews.md'
    $upTo = @{ T = -1; D = -1; REQ = -1 }
    $marked = $false
    $since = [datetime]::MinValue
    if (Test-Path -LiteralPath $review) {
        $marks = @(Read-Lines $review | Where-Object { $_ -match '(?i)Reviewed up to:' })
        if ($marks.Count -gt 0) {
            $marked = $true
            foreach ($m in [regex]::Matches(($marks -join ' '), '\b(T|D|REQ)-(\d+)\b')) { $n = [int]$m.Groups[2].Value; if ($n -gt $upTo[$m.Groups[1].Value]) { $upTo[$m.Groups[1].Value] = $n } }
            $script:scope.ReviewedUpTo = (($marks[0] -replace '(?i)^.*Reviewed up to:\s*', '') -replace '[`*]', '').Trim()
        }
        else { $since = (Get-Item -LiteralPath $review).LastWriteTime }
    }
    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    $newIds = @{ T = New-Object 'System.Collections.Generic.List[string]'; D = New-Object 'System.Collections.Generic.List[string]'; REQ = New-Object 'System.Collections.Generic.List[string]' }
    $issued = Get-IssuedIds
    foreach ($id in $issued.Keys) {
        if ($id -notmatch '^(T|D|REQ)-(\d+)$') { continue }
        $prefix = $Matches[1]; $n = [int]$Matches[2]
        if ($marked) { if ($n -gt $upTo[$prefix]) { $newIds[$prefix].Add($id) } }
        else {
            $dt = [datetime]::MinValue
            if ([datetime]::TryParseExact($issued[$id].Time, 'yyyy-MM-dd HH:mm', $culture, [System.Globalization.DateTimeStyles]::None, [ref]$dt) -and $dt -gt $since) { $newIds[$prefix].Add($id) }
        }
    }
    $script:scope.NewTasks = $newIds.T.Count; $script:scope.NewDecisions = $newIds.D.Count; $script:scope.NewRequirements = $newIds.REQ.Count
    $sanityBusy = @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'owner') -like 'sanity-agent*' -and (Get-Field $_ 'status') -ne 'Done' }).Count -gt 0
    # When: at once for a new requirement; otherwise once per sprint, in the sanity agent's sprint
    # check, before the sprint review ("Reviewed up to: ... (sprint S2)"). Without a sprint in
    # plan.md yet: after 10 new tasks or decisions.
    # Assigned first: Get-Sprints returns its array as one object.
    $allSprints = Get-Sprints
    $current = @($allSprints | Where-Object { $_.Status -match '^(Current|Review)$' } | Select-Object -First 1)
    $reviewedSprint = ''
    if ($script:scope.ReviewedUpTo -match '(?i)\bsprint\s+([A-Za-z]*\d+)\b') { $reviewedSprint = $Matches[1] }
    $why = ''
    if ($newIds.REQ.Count -gt 0) { $why = 'a new requirement' }
    elseif ($current.Count -eq 0) { if ($newIds.T.Count + $newIds.D.Count -ge 10) { $why = 'no sprint yet, 10 or more new items' } }
    elseif ($current[0].Status -eq 'Review' -and $reviewedSprint -ne $current[0].Name -and $newIds.T.Count + $newIds.D.Count -gt 0) { $why = "sprint $($current[0].Name) is in Review" }
    if (-not $sanityBusy -and $why) {
        $what = ('{0} tasks, {1} decisions and {2} requirements are new since the last scope review ({3})' -f $newIds.T.Count, $newIds.D.Count, $newIds.REQ.Count, $why)
        $mark = 'Reviewed up to:'; if ($current.Count -gt 0) { $mark = ('Reviewed up to: ... (sprint {0})' -f $current[0].Name) }
        Add-Problem 'WARNING' 'reviews/sanity-reviews.md' ('{0}: give the sanity agent the scope review (in its sprint check, or reopen its task; within the requirements, outside them, or not needed; it ends with "{1}")' -f $what, $mark)
        $script:orchestratorWork.Add(('have the sanity agent do the scope review ({0}) and start it' -f $what))
    }

    # 3. The team's active time against the time boundary in project.md.
    $script:scope.Active = Get-ActiveTime
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (-not (Test-Path -LiteralPath $projectFile)) { return }
    $checkedAt = 0.0
    foreach ($l in (Read-Lines $projectFile)) {
        if ($l -match '^\|\s*Time\b[^|]*\|\s*([^|]*)\|') { $cell = $Matches[1]; if ($cell -match '(\d+(?:[.,]\d+)?)\s*(hours?|h)\b') { $script:scope.BoundaryHours = [double]($Matches[1] -replace ',', '.') } }
        if ($l -match '^\*\*Time boundary checked:\*\*.*?\bat\s+(\d+(?:[.,]\d+)?)\s*hours?') { $checkedAt = [double]($Matches[1] -replace ',', '.') }
    }
    $b = $script:scope.BoundaryHours
    $h = $script:scope.Active.Hours
    if ($b -gt 0 -and $h -ge 0.8 * $b -and $h -gt $checkedAt + 0.1 * $b) {
        Add-Problem 'WARNING' 'project.md' ('the team has been active about {0} of its {1} hours (estimated from its activity): ask the user whether to go on, cut scope or move the boundary, and record the answer as "**Time boundary checked:** <date> at {0} hours"' -f $h, $b)
        $script:orchestratorWork.Add(('ask the user about the time boundary (active about {0} of {1} hours)' -f $h, $b))
    }
}

# --- Sprints: the delivery plan, the current sprint, work per requirement -------------------

# The requirements the item of a task serves: the requirement itself, those a decision names, or
# those a bug names.
function Get-ItemRequirements([string]$For, $Items) {
    # Returns the ids one by one (callers wrap the call in @()); a wrapped array would never match.
    if ($For -match '^REQ-\d+$') { return $For }
    if ($For -match '^D-\d+$') {
        $d = @($Items | Where-Object { $_.File -eq $For -and $_.HasHeader })
        if ($d.Count -gt 0) { return @(Get-RequirementIds ((Get-Field $d[0] 'requirements') + ' ' + (Get-Field $d[0] 'title') + ' ' + $d[0].Body)) }
    }
    if ($For -match '^BUG-\d+$') { $r = Get-BugRequirements $For $Items; if ($null -ne $r) { return @($r) } }
    return
}

# The sprints in plan.md, section Sprints: Name, Dates, Goal, Requirements, Points, Demo, Status.
function Get-Sprints {
    $sprints = New-Object 'System.Collections.Generic.List[object]'
    foreach ($r in @(Get-SectionRows (Join-Path $TeamFolder 'plan.md') 'Sprints')) {
        $cells = @($r)
        if ($cells.Count -lt 7 -or ($cells -join '') -match '\{\{') { continue }
        $sprints.Add([pscustomobject]@{
                Name = $cells[0].Trim(); Dates = $cells[1].Trim(); Goal = $cells[2].Trim()
                Requirements = @(Get-RequirementIds $cells[3]); Points = $cells[4].Trim(); Demo = $cells[5].Trim()
                # The first word: "Done (accepted by the user 2026-11-20)" is Done.
                Status = @((($cells[6] -replace '[`*]', '').Trim()) -split '[\s(]')[0]
            })
    }
    return , $sprints.ToArray()
}

function Test-Sprints($Items) {
    $script:sprintInfo = @{ Length = ''; Approved = ''; Sprints = @(); Current = $null; HasPlan = $false }
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (-not (Test-Path -LiteralPath $projectFile)) { return }
    foreach ($l in (Read-Lines $projectFile)) {
        if ($l -match '^\*\*Sprint length:\*\*\s*(.+)$') { $v = $Matches[1]; if ($v -notmatch '\{\{' -and $v -match '\d+(?:[.,]\d+)?\s*(days?|weeks?)\b') { $script:sprintInfo.Length = $Matches[0] } }
    }
    if (-not $script:sprintInfo.Length) {
        Add-Problem 'WARNING' 'project.md' 'has no sprint length: ask the user how long a sprint takes, in days or weeks, and record it as "**Sprint length:** 5 days" (see the project.md template)'
        $script:orchestratorWork.Add('ask the user how long a sprint takes (days or weeks) and record **Sprint length:** in project.md')
    }
    # No delivery plan is needed before the requirements are approved.
    if (@($Items | Where-Object { $_.Kind -eq 'requirement' -and $_.HasHeader -and (Get-Field $_ 'status') -eq 'Approved' }).Count -eq 0) { return }
    $planFile = Join-Path $TeamFolder 'plan.md'
    if (-not (Test-Path -LiteralPath $planFile)) {
        Add-Problem 'WARNING' 'plan.md' 'is missing: make the delivery plan from the template (which functionality in which sprint, per requirement; sprint 1 ends with a working first screen) and get the user''s approval before the team builds on'
        $script:orchestratorWork.Add('make the delivery plan (plan.md: the sprints per requirement, sprint 1 ends with a working first screen) and get the user''s approval')
        return
    }
    $script:sprintInfo.HasPlan = $true
    foreach ($l in (Read-Lines $planFile)) { if ($l -match '^\*\*Approved by the user:\*\*\s*(\d{4}-\d{2}-\d{2})') { $script:sprintInfo.Approved = $Matches[1] } }
    $sprints = Get-Sprints
    $script:sprintInfo.Sprints = @($sprints)
    if ($sprints.Count -eq 0) {
        Add-Problem 'WARNING' 'plan.md' 'has no sprints: fill in the Sprints table (per sprint its goal, requirements, story points, demo and status)'
        $script:orchestratorWork.Add('fill in the sprints of plan.md and get the user''s approval')
        return
    }
    if (-not $script:sprintInfo.Approved) {
        Add-Problem 'WARNING' 'plan.md' 'is not approved by the user: show the plan with the question tool and record "**Approved by the user:** <date>"'
        $script:orchestratorWork.Add('ask the user to approve the delivery plan (plan.md) with the question tool')
        $script:userSteps.Add('approve the delivery plan (plan.md): what you get in which sprint')
    }
    $current = @($sprints | Where-Object { $_.Status -match '^(Current|Review)$' } | Select-Object -First 1)
    if ($current.Count -eq 0) {
        $next = @($sprints | Where-Object { $_.Status -ne 'Done' } | Select-Object -First 1)
        if ($next.Count -gt 0) {
            Add-Problem 'WARNING' 'plan.md' ('has no Current sprint: start {0} (set it to Current and create its tasks)' -f $next[0].Name)
            $script:orchestratorWork.Add(('start sprint {0}: set it to Current in plan.md and create the tasks of its requirements' -f $next[0].Name))
        }
        return
    }
    $cur = $current[0]
    $script:sprintInfo.Current = $cur
    if ($cur.Status -eq 'Review') {
        $script:userSteps.Add(('sprint review of {0} ({1}): see it working ({2}), then accept it or ask for changes in the team''s chat' -f $cur.Name, $cur.Goal, $cur.Demo))
        $script:orchestratorWork.Add(('ask the user for the sprint review of {0} with the question tool (its goal and how to see it working), record the verdict under Sprint reviews and set it to Done' -f $cur.Name))
    }
    # Unfinished tasks for requirements of another sprint: nobody works on them now, except the
    # refinement of the next sprint (requirements, design, questions) by the roles without code.
    $inSprint = @{}
    foreach ($r in $cur.Requirements) { $inSprint[$r] = $true }
    $inNext = @{}
    $curIndex = [Array]::IndexOf(@($sprints), $cur)
    $nextSprint = @($sprints | Select-Object -Skip ($curIndex + 1) | Where-Object { $_.Status -ne 'Done' } | Select-Object -First 1)
    if ($nextSprint.Count -gt 0) { foreach ($r in $nextSprint[0].Requirements) { $inNext[$r] = $true } }
    $later = New-Object 'System.Collections.Generic.List[string]'
    $sprintOpen = 0
    foreach ($t in @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'status') -ne 'Done' })) {
        $for = Get-Field $t 'for'
        if (-not $for -or $for -eq 'team') { continue }
        $reqs = @(Get-ItemRequirements $for $Items)
        if ($reqs.Count -eq 0) { continue }
        if (@($reqs | Where-Object { $inSprint.ContainsKey($_) }).Count -gt 0) { $sprintOpen++ }
        elseif ((Get-Field $t 'owner') -match '^(architect|analyst|designer|sanity-agent)(-\d+)?$' -and @($reqs | Where-Object { $inNext.ContainsKey($_) }).Count -gt 0) { continue }
        else { $later.Add($t.File); $script:notNow[$t.File] = $true }
    }
    if ($later.Count -gt 0) {
        Add-Problem 'WARNING' 'plan.md' ('{0} serve no requirement of the current sprint {1}: nobody works on them now (RULES.md, rule 15). They wait for their sprint; to change that, ask the user to change the plan' -f ($later -join ', '), $cur.Name)
    }
    if ($cur.Status -eq 'Current' -and $sprintOpen -eq 0) {
        $script:orchestratorWork.Add(('sprint {0} has no open tasks: create the tasks of its requirements, or, when they are built, run the assessment, set it to Review and show the user the working result' -f $cur.Name))
    }
}

# With a ticketing system in project.md, every item in work (a requirement or request with
# unfinished tasks or an open pull request, a bug with an unfinished fix task) has its own ticket.
function Test-Tickets($Items) {
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (-not (Test-Path -LiteralPath $projectFile)) { return }
    $system = ''; $allowed = $false
    foreach ($l in (Read-Lines $projectFile)) {
        if ($l -match '^\|\s*System \(for example Jira[^|]*\|\s*([^|]*)\|') { $system = $Matches[1].Trim() }
        if ($l -match '^\|\s*Tickets created and updated by the orchestrator[^|]*\|\s*yes\b') { $allowed = $true }
    }
    if (-not $system -or $system -match '\{\{' -or $system -match '^(?i)(none|no|unknown|-|n/a)\b') { return }
    $keyPattern = '(?<![\w-])(?!(?:BUG|REQ|US|D|T|Q|S|F|B|U)-\d)[A-Z][A-Z0-9]{1,9}-\d+\b|(?:AB)?#\d+\b'
    $missing = New-Object 'System.Collections.Generic.List[string]'
    $unfinished = @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'status') -ne 'Done' })
    $inWork = @{}
    # Only items of the current sprint: tasks that may not start now need no ticket yet.
    foreach ($t in $unfinished) {
        if ($script:notNow.ContainsKey((Get-Field $t 'id'))) { continue }
        $f = Get-Field $t 'for'
        if ($f -match '^(REQ|D|BUG)-\d+$') { $inWork[$f] = $true }
    }
    foreach ($it in @($Items | Where-Object { $_.HasHeader -and $_.Kind -in @('requirement', 'decision') })) {
        $pr = Get-Field $it 'pr'
        if (-not ($inWork.ContainsKey($it.File) -or ($pr -match 'https?://' -and -not (Get-Field $it 'merged')))) { continue }
        if (-not (Get-Field $it 'ticket')) { $missing.Add($it.File) }
    }
    foreach ($bug in @($inWork.Keys | Where-Object { $_ -like 'BUG-*' } | Sort-Object)) {
        $row = ''
        foreach ($r in @(Get-SectionRows (Join-Path $TeamFolder 'testing.md') 'Bugs')) { $line = ($r -join ' | '); if ($line -match ('(^|\|)\s*' + [regex]::Escape($bug) + '\s*(\||$)')) { $row = $line; break } }
        if ($row -notmatch $keyPattern) { $missing.Add($bug) }
    }
    if ($missing.Count -eq 0) { return }
    $how = 'create it'
    if (-not $allowed) { $how = 'give the user the ticket text to create (project.md has no OK for the orchestrator to create tickets)' }
    Add-Problem 'WARNING' 'project.md' ('{0} in work without a ticket in {1}: {2} (what, how, why; SKILL.md, "Version control and pull requests", step 7), record its key (ticket: in the header, or the Ticket column of a bug) and link the pull request to it' -f ($missing -join ', '), $system, $how)
    $script:orchestratorWork.Add(('tickets for {0}: {1}, record the keys and link the pull requests' -f ($missing -join ', '), $how))
}

# Team setup files (agent definitions, prompt files, the solution file) belong to no task, so no
# agent commits them: when project.md allows commits, uncommitted ones are the orchestrator's.
function Test-TeamSetupCommits {
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (-not (Test-Path -LiteralPath $projectFile)) { return }
    if (@(Read-Lines $projectFile | Where-Object { $_ -match '^\|\s*Commits and pushes by the agents[^|]*\|\s*yes\b' }).Count -eq 0) { return }
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { return }
    $repoRoot = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Parent
    $paths = @('.github/agents', '.github/prompts', '.claude/agents') +
        @(Get-ChildItem -LiteralPath $repoRoot -File | Where-Object { $_.Extension -in @('.sln', '.slnx') } | ForEach-Object { $_.Name })
    $lines = @()
    $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $lines = @(& git -C $repoRoot status --porcelain --untracked-files=all -- $paths 2>$null) } catch { $lines = @() } finally { $ErrorActionPreference = $previous }
    $files = @($lines | Where-Object { $_ -and $_.Length -gt 3 } | ForEach-Object { $_.Substring(3).Trim('"') })
    if ($files.Count -eq 0) { return }
    $more = ''
    if ($files.Count -gt 4) { $more = (' and {0} more' -f ($files.Count - 4)) }
    Add-Problem 'WARNING' 'version control' ('team setup files not committed: {0}{1}. They belong to no task, so the orchestrator commits them at once in a commit of their own (only these paths, Agent: orchestrator) and pushes it (SKILL.md, "Version control and pull requests", step 6)' -f ((@($files | Select-Object -First 4)) -join ', '), $more)
    $script:orchestratorWork.Add(('commit the team setup changes ({0} file(s)) in a commit of their own and push it' -f $files.Count))
}

# The sections every document must have (its template's "## " headings).
$documentSections = [ordered]@{
    'project.md'      = @('Team', 'Customer request', 'Project environment')
    'analysis.md'     = @('Problem and goals', 'Stakeholders and user groups', 'User stories', 'Success measures', 'Acceptance check')
    'architecture.md' = @('Architecture diagram', 'Layers and components', 'API structure', 'Error handling', 'Technology versions', 'Database architecture', 'Authentication and authorization', 'Build task list')
    'design.md'       = @('User flows', 'Screens', 'Design reviews')
    'testing.md'      = @('Test plan', 'Test runs', 'Bugs')
    'plan.md'         = @('Sprints', 'Sprint reviews')
}

function Test-DocumentSections {
    # analysis.md and design.md only matter when the analyst or designer is in the team.
    $team = ''
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (Test-Path -LiteralPath $projectFile) { $team = [System.IO.File]::ReadAllText($projectFile) }
    foreach ($d in $documentSections.Keys) {
        $path = Join-Path $TeamFolder $d
        if (-not (Test-Path -LiteralPath $path)) { continue }
        if ($d -eq 'analysis.md' -and $team -notmatch '(?m)^\|\s*analyst\s*\|') { continue }
        if ($d -eq 'design.md' -and $team -notmatch '(?m)^\|\s*designer\s*\|') { continue }
        $headings = @(Read-Lines $path | Where-Object { $_ -match '^##\s+' } | ForEach-Object { ($_ -replace '^##\s+', '').Trim() })
        $missing = @($documentSections[$d] | Where-Object { $name = $_; -not @($headings | Where-Object { $_ -like "$name*" }).Count })
        if ($missing.Count -eq 0) { continue }
        Add-Problem 'WARNING' $d ('lacks the sections: {0}. Add them from the skill''s template (empty), and let the owner fill them in' -f ($missing -join ', '))
        $script:orchestratorWork.Add("add the missing sections to $d")
    }
}

# Documents (project.md, architecture.md, reviews/..., architecture/...) that any checked copy has and the team
# folder lacks. Every copy counts, because a copy can be saved after a document was lost.
# A document removed on purpose is listed in archive/done/removed.md.
function Get-MissingDocuments([string]$BaselineFolder) {
    $missing = New-Object 'System.Collections.Generic.List[string]'
    $copies = @(Get-OkCopies | ForEach-Object { $_.FullName })
    if ($BaselineFolder -and (Test-Path -LiteralPath $BaselineFolder) -and $copies -notcontains $BaselineFolder) { $copies += $BaselineFolder }
    $removed = ''
    $removedFile = Join-Path (Join-Path (Join-Path $TeamFolder 'archive') 'done') 'removed.md'
    if (Test-Path -LiteralPath $removedFile) { $removed = [System.IO.File]::ReadAllText($removedFile) }
    $names = New-Object 'System.Collections.Generic.List[string]'
    foreach ($c in $copies) {
        foreach ($d in $documents) { if (-not $names.Contains($d) -and (Test-Path -LiteralPath (Join-Path $c $d))) { $names.Add($d) } }
        foreach ($n in @(Get-FolderDocuments $c)) { if (-not $names.Contains($n)) { $names.Add($n) } }
    }
    foreach ($n in $names) {
        if (Test-Path -LiteralPath (Join-Path $TeamFolder $n)) { continue }
        if ($removed -match ('(?m)^\s*[-*]\s.*' + [regex]::Escape(($n -replace '^reviews/', '')))) { continue }
        $missing.Add($n)
    }
    return $missing
}

# The checked copies (manual -ok and automatic -auto-ok) that hold the new layout, newest first.
function Get-OkCopies {
    $dir = Join-Path $TeamFolder 'archive'
    if (-not (Test-Path -LiteralPath $dir)) { return @() }
    return @(Get-ChildItem -LiteralPath $dir -Directory | Where-Object { $_.Name -like '*-ok' -and (Test-Path -LiteralPath (Join-Path $_.FullName 'project.md')) } | Sort-Object Name -Descending)
}

# Restores lost or damaged files: "missing" (everything the check finds lost or empty) or names
# separated by commas (T-026, architecture.md, architecture/database.md; a file name alone such
# as database.md is found in the one folder that has it). A file comes back from the newest checked copy
# that has it; an item no copy has is recreated under the same id from ids.log, for its owner to
# fill in again. An existing good file is never overwritten; a damaged one is kept as evidence.
function Invoke-Restore([string]$Names) {
    $items = @(Get-Items $TeamFolder)
    $targets = New-Object 'System.Collections.Generic.List[string]'
    if ($Names -eq 'missing') {
        $copies = Get-OkCopies
        $base = $null; if ($copies.Count -gt 0) { $base = $copies[0].FullName }
        Test-Gaps $items
        foreach ($id in $script:missingIds) { $targets.Add($id) }
        foreach ($d in (Get-MissingDocuments $base)) { $targets.Add($d) }
        if (-not (Test-Path -LiteralPath (Join-Path $TeamFolder 'project.md')) -and -not $targets.Contains('project.md')) { $targets.Add('project.md') }
        foreach ($it in $items) { if ($it.Empty -or -not $it.HasHeader) { $targets.Add($it.File) } }
        foreach ($d in (Get-DocumentNames)) { if (@(Read-Lines (Join-Path $TeamFolder $d) | Where-Object { $_.Trim() }).Count -eq 0) { $targets.Add($d) } }
        # Start the check afresh after the restore.
        $script:errors.Clear(); $script:warnings.Clear(); $script:orchestratorWork.Clear(); $script:somethingLost = $false
    } else {
        foreach ($n in @($Names -split '\s*,\s*' | Where-Object { $_ })) { $targets.Add($n) }
    }
    if ($targets.Count -eq 0) { Write-Output 'Nothing to restore.'; return }

    $issued = Get-IssuedIds
    $stamp = Get-Date -Format 'yyyy-MM-dd-HHmm'
    foreach ($name in $targets) {
        # Where the file lives in the team folder.
        $kind = $null
        foreach ($k in $kinds.Keys) { if ($name -match ('^' + $kinds[$k].Prefix + '-\d+$')) { $kind = $k } }
        if ($kind) { $rel = "$($kinds[$kind].Folder)/$name.md" }
        elseif ($name -match '/') { $rel = $name }
        elseif ($documents -contains $name) { $rel = $name }
        else {
            # A file name alone (database.md): the one folder (reviews/, architecture/, ...) that
            # has it now or in a checked copy; reviews/ when none has it.
            $rel = "reviews/$name"
            $found = @(@(Get-FolderDocuments $TeamFolder) + @(Get-OkCopies | ForEach-Object { Get-FolderDocuments $_.FullName }) | Where-Object { (Split-Path $_ -Leaf) -eq $name } | Sort-Object -Unique)
            if ($found.Count -gt 1) { Write-Output ("$name is in more than one folder ({0}): name it with its folder" -f ($found -join ', ')); continue }
            if ($found.Count -eq 1) { $rel = $found[0] }
        }
        $path = Join-Path $TeamFolder $rel

        if (Test-Path -LiteralPath $path) {
            $text = [System.IO.File]::ReadAllText($path)
            $damaged = -not $text.Trim()
            if ($kind -and -not $damaged) { $damaged = -not (Read-Item $path $kind).HasHeader }
            if (-not $damaged) { Write-Output "$rel exists and is not damaged: not overwritten"; continue }
            $evidence = Join-Path (Join-Path $TeamFolder 'archive') "$stamp-damaged-do-not-restore"
            New-Item -ItemType Directory -Path (Join-Path $evidence (Split-Path $rel -Parent)) -Force | Out-Null
            Move-Item -LiteralPath $path -Destination (Join-Path $evidence $rel) -Force
            Write-Output "Moved the damaged $rel to archive/$stamp-damaged-do-not-restore/"
        }

        $source = $null
        foreach ($c in (Get-OkCopies)) { $p = Join-Path $c.FullName $rel; if (Test-Path -LiteralPath $p) { $source = $c; break } }
        if ($source) {
            New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
            Copy-Item -LiteralPath (Join-Path $source.FullName $rel) -Destination $path
            Write-Output "Restored $rel from archive/$($source.Name)"
            continue
        }
        if ($kind -and $issued.Contains($name)) {
            $reg = $issued[$name]
            $note = "(Recreated by -Restore on $(Get-Date -Format 'yyyy-MM-dd HH:mm'): the file was lost and no checked copy had it. Its owner fills it in again.)"
            $today = Get-Date -Format 'yyyy-MM-dd'
            switch ($kind) {
                'task' { $header = @("title: $($reg.Title)", "owner: $($reg.Who)", 'status: Open', 'waits-for: ', "created: $today"); $body = "## Task`n`n$($reg.Title)`n`n$note`n`n## Plan`n`n## Report`n" }
                'question' {
                    $from = $reg.Who; $to = 'orchestrator'
                    if ($reg.Who -match '^(.+?)\s*->\s*(.+)$') { $from = $Matches[1]; $to = $Matches[2] }
                    $header = @("title: $($reg.Title)", "from: $from", "to: $to", 'status: Open', "created: $today"); $body = "## Question`n`n$($reg.Title)`n`n$note`n`n## Answer`n"
                }
                'decision' { $header = @("title: $($reg.Title)", "who: $($reg.Who)", 'status: Proposed', "date: $today", 'files: '); $body = "## Decision`n`n$($reg.Title)`n`n$note`n`n## Why`n" }
                'requirement' { $header = @("title: $($reg.Title)", 'type: unknown', 'priority: unknown', 'status: Proposed', 'source: '); $body = "## Requirement`n`n$($reg.Title)`n`n$note`n`n## Acceptance criteria`n" }
            }
            New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
            Write-TextFile $path ("---`nid: $name`n" + ($header -join "`n") + "`n---`n`n" + $body)
            Write-Output "Recreated $rel from ids.log (no checked copy had it): $($reg.Who) fills it in again"
            continue
        }
        Write-Output "NOT FOUND $rel is in no checked copy and not in ids.log: recreate it by hand, or list it in archive/done/removed.md"
    }
}

# In progress means an agent runs on it now and writes a Progress line after every step, so a
# task file that has not changed for SilenceMinutes is a run that hangs or has ended.
function Test-Heartbeat($Items) {
    foreach ($t in @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'status') -eq 'In progress' })) {
        $minutes = ((Get-Date) - (Get-Item -LiteralPath $t.Path).LastWriteTime).TotalMinutes
        if ($minutes -lt $SilenceMinutes) { continue }
        $owner = Get-Field $t 'owner'
        Add-Problem 'WARNING' (Get-RelativePath $t.Path) ('{0} is In progress but shows no sign of life for {1:N0} minutes ({2}): its run waits for an Allow click or a command that does not end, or it has ended. Check its chat; when it no longer runs, set it back to Open and start {2} again' -f $t.File, $minutes, $owner)
        $script:orchestratorWork.Add(('{0} of {1} shows no sign of life for {2:N0} minutes: when no sub-agent of yours runs, set it back to Open and start {1} again; otherwise ask the user to check its chat for an Allow click' -f $t.File, $owner, $minutes))
    }
}

function Test-Locks($Locks, $Items) {
    $ids = @{}
    foreach ($it in $Items) { $ids[$it.File] = $true }
    foreach ($l in $Locks) {
        if ($l.Minutes -ge $LockMinutes) {
            Add-Problem 'WARNING' "locks/$($l.Name).lock" ("is stale: {0} has held it for {1:N0} minutes. Release it with -Unlock {2} -Owner orchestrator -Force when that agent is not running (at the start of your turn none of your sub-agents runs)" -f $l.Owner, $l.Minutes, $l.Name)
            $script:orchestratorWork.Add("release the stale lock on $($l.Name) of $($l.Owner) now when no sub-agent of yours runs (-Unlock $($l.Name) -Owner orchestrator -Force), and set its In progress tasks back to Open")
        }
        # A document is locked by its file name (architecture.md, security-assessments.md), wherever it is outside the archive.
        $exists = $ids.ContainsKey($l.Name)
        if (-not $exists -and $l.Name -like '*.md') {
            $exists = @(Get-ChildItem -LiteralPath $TeamFolder -Recurse -Filter $l.Name -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '[\\/]archive[\\/](?!done[\\/])' }).Count -gt 0
        }
        if (-not $exists) { Add-Problem 'WARNING' "locks/$($l.Name).lock" 'locks a file that does not exist' }
    }
}

function Compare-WithBaseline($Items, [string]$BaselineFolder) {
    $base = Get-Items $BaselineFolder
    $now = @{}
    foreach ($it in $Items) { $now[$it.File] = $true }
    $done = Get-GoneOnPurpose
    foreach ($b in $base) {
        if ($now.ContainsKey($b.File) -or $done.ContainsKey($b.File)) { continue }
        $script:somethingLost = $true
        # Already reported as a missing number.
        if ($script:missingIds -and $script:missingIds.Contains($b.File)) { continue }
        if ($b.Kind -in @('decision', 'requirement')) { Add-Problem 'ERROR' "$($kinds[$b.Kind].Folder)/$($b.File).md" 'is in the -ok copy but missing now: restore it' }
        else { Add-Problem 'WARNING' "$($kinds[$b.Kind].Folder)/$($b.File).md" 'is in the -ok copy but missing now (fine only when compaction moved it to archive/done)' }
    }
}

# --- Team status and overview ----------------------------------------------------------------

function Get-GoCommand([string]$Instance) {
    if ($Instance -eq 'orchestrator') { return 'orchestrator' }
    if ($Instance -match '^(.+)-(\d+)$') {
        if ($Matches[2] -eq '1') { return "@$($Matches[1]) go" }
        return "@$($Matches[1]) go $($Matches[2])"
    }
    return "@$Instance go"
}

function Join-OrDash($Items) {
    $text = (@($Items) -join ', ')
    if ($text) { return $text } else { return '-' }
}

function Get-TeamStatus($Items) {
    $tasks = @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader })
    $openQuestions = @{}
    foreach ($q in @($Items | Where-Object { $_.Kind -eq 'question' -and $_.HasHeader -and (Get-Field $_ 'status') -eq 'Open' })) { $openQuestions[(Get-Field $q 'id')] = $q }
    $owner = @{}; $status = @{}
    foreach ($t in $tasks) { $owner[(Get-Field $t 'id')] = (Get-Field $t 'owner'); $status[(Get-Field $t 'id')] = (Get-Field $t 'status') }

    $instances = New-Object 'System.Collections.Generic.List[string]'
    foreach ($t in $tasks) { $o = Get-Field $t 'owner'; if ($o -and -not $instances.Contains($o)) { $instances.Add($o) } }
    # A question to a role goes to its first instance.
    $resolve = { param($n) if ($instances.Contains($n)) { $n } elseif ($instances.Contains("$n-1")) { "$n-1" } else { $n } }
    foreach ($q in $openQuestions.Values) {
        foreach ($n in @((Get-Field $q 'from'), (Get-Field $q 'to'))) { $r = & $resolve $n; if ($r -and $r -ne 'user' -and -not $instances.Contains($r)) { $instances.Add($r) } }
    }

    $rows = New-Object 'System.Collections.Generic.List[object]'
    foreach ($name in $instances) {
        $mine = @($tasks | Where-Object { (Get-Field $_ 'owner') -eq $name })
        $blockedBy = New-Object 'System.Collections.Generic.List[string]'
        $waits = New-Object 'System.Collections.Generic.List[string]'
        foreach ($q in $openQuestions.Values) { if ((& $resolve (Get-Field $q 'from')) -eq $name) { $waits.Add((Get-Field $q 'id')) } }
        foreach ($t in @($mine | Where-Object { (Get-Field $_ 'status') -eq 'Blocked' })) {
            $w = Get-ItemWaits (Get-Field $t 'waits-for')
            foreach ($n in $w.Tasks) { if ($owner.ContainsKey($n) -and $status[$n] -ne 'Done' -and $owner[$n] -ne $name -and -not $blockedBy.Contains($owner[$n])) { $blockedBy.Add($owner[$n]) } }
            foreach ($n in $w.Questions) {
                if ($openQuestions.ContainsKey($n)) {
                    if (-not $waits.Contains($n)) { $waits.Add($n) }
                    $who = & $resolve (Get-Field $openQuestions[$n] 'to')
                    if ($who -and $who -ne $name -and -not $blockedBy.Contains($who)) { $blockedBy.Add($who) }
                }
            }
            if ($w.User -and -not $blockedBy.Contains('user')) { $blockedBy.Add('user') }
        }
        $must = @($openQuestions.Values | Where-Object { (& $resolve (Get-Field $_ 'to')) -eq $name } | ForEach-Object { Get-Field $_ 'id' } | Sort-Object)
        $waitingOnMe = New-Object 'System.Collections.Generic.List[string]'
        foreach ($t in $tasks) {
            if ((Get-Field $t 'status') -ne 'Blocked' -or (Get-Field $t 'owner') -eq $name) { continue }
            $w = Get-ItemWaits (Get-Field $t 'waits-for')
            $hit = @($w.Tasks | Where-Object { $owner[$_] -eq $name -and $status[$_] -ne 'Done' }).Count +
                @($w.Questions | Where-Object { $openQuestions.ContainsKey($_) -and (& $resolve (Get-Field $openQuestions[$_] 'to')) -eq $name }).Count
            if ($hit -gt 0) { $waitingOnMe.Add((Get-Field $t 'id')) }
        }
        $open = @($mine | Where-Object { (Get-Field $_ 'status') -eq 'Open' -and -not $script:notNow.ContainsKey((Get-Field $_ 'id')) }).Count
        $working = @($mine | Where-Object { (Get-Field $_ 'status') -eq 'In progress' }).Count
        $blocked = @($mine | Where-Object { (Get-Field $_ 'status') -eq 'Blocked' }).Count
        $done = @($mine | Where-Object { (Get-Field $_ 'status') -eq 'Done' }).Count
        if ($working -gt 0) { $state = 'Working' }
        elseif ($must.Count -gt 0) { $state = 'Must answer questions' }
        elseif ($open -gt 0) { $state = 'Ready' }
        elseif ($waits.Count -gt 0) { $state = 'Stalled: waits for answers' }
        elseif ($blocked -gt 0) { $state = 'Blocked' }
        elseif ($done -gt 0) { $state = 'Done' }
        else { $state = 'Idle' }
        $rows.Add([pscustomobject]@{ Name = $name; State = $state; Open = $open; Working = $working; Blocked = $blocked; Done = $done
                BlockedBy = @($blockedBy); WaitingOnMe = @($waitingOnMe | Sort-Object); Waits = @($waits | Sort-Object); Must = @($must) })
    }
    $forUser = @($openQuestions.Values | Where-Object { (Get-Field $_ 'to') -eq 'user' } | ForEach-Object { Get-Field $_ 'id' } | Sort-Object)
    return [pscustomobject]@{ Rows = $rows; ForUser = $forUser }
}

function Get-StatusLines($Team, $Locks) {
    $out = New-Object 'System.Collections.Generic.List[string]'
    $out.Add('| Instance | State | Open | In progress | Blocked | Done | Blocked by | Others wait for it | Waits for answers to | Must answer |')
    $out.Add('|---|---|---|---|---|---|---|---|---|---|')
    foreach ($r in $Team.Rows) {
        $out.Add(('| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} | {9} |' -f $r.Name, $r.State, $r.Open, $r.Working, $r.Blocked, $r.Done,
                (Join-OrDash $r.BlockedBy), (Join-OrDash $r.WaitingOnMe), (Join-OrDash $r.Waits), (Join-OrDash $r.Must)))
    }
    if ($Team.ForUser.Count -gt 0) { $userState = 'Waiting for you' } else { $userState = '-' }
    $out.Add(('| user | {0} | - | - | - | - | - | - | - | {1} |' -f $userState, (Join-OrDash $Team.ForUser)))
    $out.Add('')
    $lockText = Join-OrDash (@($Locks | Where-Object { $_ -and $_.Name }) | ForEach-Object { '{0} ({1}, since {2:HH:mm})' -f $_.Name, $_.Owner, $_.Time })
    $out.Add("Locked now: $lockText")
    $out.Add('')
    $out.Add('**How to proceed** (start them in this order: the first ones unblock others):')
    $out.Add('')
    $st = $script:stopState
    if ($st -and $st.Mode -eq 'now') {
        $out.Add(('1. orchestrator: **emergency stop** since {0} ({1}: {2}). Start no agent and run no other command: no commit, push or merge. Roles still running stop at their next step (stop.md). To go on: `team continue` (it ends the stop with -Resume).' -f $st.Since, $st.By, $st.Reason))
        return $out
    }
    if ($st) {
        $running = @($Team.Rows | Where-Object { $_.Working -gt 0 } | ForEach-Object { $_.Name })
        $out.Add(('1. orchestrator: **team stop** since {0} ({1}: {2}). Start no new work. Wait for the roles that are working ({3}), then follow "Stopping the team" in the skill: commit, have the pull requests reviewed, ask the user to approve them, merge.' -f $st.Since, $st.By, $st.Reason, (Join-OrDash $running)))
        $step = 1
        foreach ($u in $script:readyPullRequests) { $step++; $out.Add(('{0}. You: review on GitHub and approve in the chat (the orchestrator then merges): {1}' -f $step, $u)) }
        return $out
    }
    $step = 0
    # The orchestrator's repairs come first: nobody should work on a damaged or outdated team folder.
    $repairs = New-Object 'System.Collections.Generic.List[string]'
    if ($script:errors.Count -gt 0) { $repairs.Add("repair the $($script:errors.Count) error(s) under Problems") }
    foreach ($w in $script:orchestratorWork) { if (-not $repairs.Contains($w)) { $repairs.Add($w) } }
    if ($script:toUnblock.Count -gt 0) { $repairs.Add('set ' + ($script:toUnblock -join ', ') + ' to Open (-Unblock)') }
    if ($repairs.Count -gt 0) {
        $step++
        $out.Add(('{0}. orchestrator, first: {1}' -f $step, ($repairs -join '; ')))
    }
    $order = @($Team.Rows | Where-Object { $_.Must.Count -gt 0 -or $_.Open -gt 0 -or $_.Working -gt 0 } |
            Sort-Object @{ Expression = { $_.Must.Count -gt 0 }; Descending = $true }, @{ Expression = { $_.WaitingOnMe.Count }; Descending = $true }, @{ Expression = { $_.Open }; Descending = $true })
    foreach ($r in $order) {
        $step++
        $what = New-Object 'System.Collections.Generic.List[string]'
        if ($r.Must.Count -gt 0) { $what.Add('answer ' + ($r.Must -join ', ')) }
        if ($r.Working -gt 0) { $what.Add('finish the task in progress') }
        if ($r.Open -gt 0) { $what.Add("$($r.Open) open task(s)") }
        if ($r.WaitingOnMe.Count -gt 0) { $what.Add('unblocks ' + ($r.WaitingOnMe -join ', ')) }
        $out.Add(('{0}. `{1}`: {2}' -f $step, (Get-GoCommand $r.Name), ($what -join '; ')))
    }
    if ($Team.ForUser.Count -gt 0) { $step++; $out.Add(('{0}. You: answer {1} with `team answer <id> <answer>`' -f $step, ($Team.ForUser -join ', '))) }
    if ($script:readyPullRequests.Count -gt 0) { $step++; $out.Add(('{0}. You: review on GitHub and approve in the chat (the orchestrator then merges): {1}' -f $step, ($script:readyPullRequests -join '; '))) }
    foreach ($u in $script:userSteps) { $step++; $out.Add(('{0}. You: {1}' -f $step, $u)) }
    if ($step -eq 0) { $out.Add('- Nobody can continue: every task left is blocked. Follow "Blocked by" to the start of the chain and break it.') }
    return $out
}

function Build-Overview($Items, $Team, $Locks) {
    $projectTitle = 'Project'
    $projectFile = Join-Path $TeamFolder 'project.md'
    if (Test-Path -LiteralPath $projectFile) {
        $first = @(Read-Lines $projectFile | Where-Object { $_ -match '^# ' } | Select-Object -First 1)
        if ($first.Count -gt 0) { $projectTitle = ($first[0] -replace '^#\s*(Project:\s*)?', '').Trim() }
    }
    $byKind = @{}
    foreach ($k in $kinds.Keys) { $byKind[$k] = @($Items | Where-Object { $_.Kind -eq $k -and $_.HasHeader } | Sort-Object { Get-IdNumber (Get-Field $_ 'id') }) }
    $link = { param($it) '[{0}]({1})' -f (Get-Field $it 'id'), (Get-RelativePath $it.Path) }

    $o = New-Object 'System.Collections.Generic.List[string]'
    $o.Add("# Team overview: $projectTitle")
    $o.Add('')
    $o.Add(('_Generated by check-decisions.ps1 on {0}. Do not edit this file: change the item files and run the script with -Build._' -f (Get-Date -Format 'yyyy-MM-dd HH:mm')))
    $o.Add('')
    $o.Add('## Waiting for you')
    $o.Add('')
    $waiting = 0
    foreach ($q in @($byKind.question | Where-Object { (Get-Field $_ 'status') -eq 'Open' -and (Get-Field $_ 'to') -eq 'user' })) { $o.Add(('- {0}: {1}' -f (& $link $q), (Get-Field $q 'title'))); $waiting++ }
    foreach ($t in @($byKind.task | Where-Object { (Get-Field $_ 'status') -eq 'Blocked' -and (Get-ItemWaits (Get-Field $_ 'waits-for')).User })) { $o.Add(('- {0} waits for you: {1}' -f (& $link $t), (Get-Field $t 'title'))); $waiting++ }
    $toApprove = @($byKind.requirement | Where-Object { (Get-Field $_ 'status') -eq 'Proposed' })
    if ($toApprove.Count -gt 0) { $o.Add('- Approve the requirements: ' + (($toApprove | ForEach-Object { & $link $_ }) -join ', ')); $waiting++ }
    if ($waiting -eq 0) { $o.Add('- Nothing is waiting for you.') }
    $o.Add('')
    $o.Add('## Problems')
    $o.Add('')
    if ($script:errors.Count + $script:warnings.Count -eq 0) { $o.Add('- No problems found.') }
    foreach ($e in $script:errors) { $o.Add('- ' + ($e -replace '\s+', ' ')) }
    foreach ($w in $script:warnings) { $o.Add('- ' + ($w -replace '\s+', ' ')) }
    $o.Add('')
    $o.Add('## Team status')
    $o.Add('')
    foreach ($l in (Get-StatusLines $Team $Locks)) { $o.Add($l) }
    $o.Add('')
    $o.Add('## Tasks')
    $o.Add('')
    $o.Add('| Task | Owner | Status | For | Waits for | Title |')
    $o.Add('|---|---|---|---|---|---|')
    foreach ($t in @($byKind.task | Where-Object { (Get-Field $_ 'status') -ne 'Done' })) {
        $o.Add(('| {0} | {1} | {2} | {3} | {4} | {5} |' -f (& $link $t), (Get-Field $t 'owner'), (Get-Field $t 'status'), (Format-Cell (Get-Field $t 'for')), (Format-Cell (Get-Field $t 'waits-for')), (Format-TitleCell (Get-Field $t 'title'))))
    }
    $o.Add('')
    $o.Add('Done: ' + (Join-OrDash (@($byKind.task | Where-Object { (Get-Field $_ 'status') -eq 'Done' }) | ForEach-Object { & $link $_ })))
    $o.Add('')
    $o.Add('## Open questions')
    $o.Add('')
    $o.Add('| Question | From | To | Title |')
    $o.Add('|---|---|---|---|')
    foreach ($q in @($byKind.question | Where-Object { (Get-Field $_ 'status') -eq 'Open' })) {
        $o.Add(('| {0} | {1} | {2} | {3} |' -f (& $link $q), (Get-Field $q 'from'), (Get-Field $q 'to'), (Format-Cell (Get-Field $q 'title'))))
    }
    $o.Add('')
    $o.Add('Closed: ' + (Join-OrDash (@($byKind.question | Where-Object { (Get-Field $_ 'status') -eq 'Closed' }) | ForEach-Object { & $link $_ })))
    $o.Add('')
    $o.Add('## Decisions')
    $o.Add('')
    $o.Add('| Decision | Status | Who | Pull request | Title |')
    $o.Add('|---|---|---|---|---|')
    foreach ($d in $byKind.decision) { $o.Add(('| {0} | {1} | {2} | {3} | {4} |' -f (& $link $d), (Format-Cell (Get-Field $d 'status')), (Get-Field $d 'who'), (Format-Cell (Get-Field $d 'pr')), (Format-TitleCell (Get-Field $d 'title')))) }
    $o.Add('')
    $o.Add('## Requirements')
    $o.Add('')
    $o.Add('| Requirement | Type | Priority | Status | Pull request | Title |')
    $o.Add('|---|---|---|---|---|---|')
    foreach ($r in $byKind.requirement) { $o.Add(('| {0} | {1} | {2} | {3} | {4} | {5} |' -f (& $link $r), (Format-Cell (Get-Field $r 'type')), (Format-Cell (Get-Field $r 'priority')), (Get-Field $r 'status'), (Format-Cell (Get-Field $r 'pr')), (Format-TitleCell (Get-Field $r 'title')))) }
    $o.Add('')
    $o.Add('## Documents')
    $o.Add('')
    $o.Add("- [report.md](report.md): the project manager overview (generated)")
    foreach ($d in (Get-DocumentNames)) { $o.Add("- [$d]($d)") }
    $archived = Get-ArchivedDone
    if ($archived.Count -gt 0) { $o.Add("- archive/done/: $($archived.Count) finished tasks and closed questions, moved there by compaction") }
    $o.Add('')

    $target = Join-Path $TeamFolder 'decisions.md'
    # A temporary file per process, so two agents building at the same time do not collide.
    $temp = "$target.$PID.tmp"
    Write-TextFile $temp (($o -join "`n") + "`n")
    Move-Item -LiteralPath $temp -Destination $target -Force
}

# --- Checked copies --------------------------------------------------------------------------

# Copies the documents, the documents' folders (architecture/) and all item folders to
# archive/<yyyy-MM-dd-HHmm><Suffix>/ and returns the path.
function Save-OkCopy([string]$Suffix) {
    $archiveDir = Join-Path $TeamFolder 'archive'
    $target = Join-Path $archiveDir ((Get-Date -Format 'yyyy-MM-dd-HHmm') + $Suffix)
    if (Test-Path -LiteralPath $target) { $target = Join-Path $archiveDir ((Get-Date -Format 'yyyy-MM-dd-HHmmss') + $Suffix) }
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    foreach ($d in $documents) { $p = Join-Path $TeamFolder $d; if (Test-Path -LiteralPath $p) { Copy-Item -LiteralPath $p -Destination $target } }
    foreach ($folder in @(@('tasks', 'questions', 'decisions', 'requirements', 'reviews') + $documentFolders)) {
        $p = Join-Path $TeamFolder $folder
        if (Test-Path -LiteralPath $p) { Copy-Item -LiteralPath $p -Destination $target -Recurse }
    }
    return $target
}

# --- Stopping the team -----------------------------------------------------------------------

# stop.md in the team folder: "mode: finish" (team stop) or "mode: now" (team emergency stop).
# Every agent checks it at every step; $null when the team is not stopped.
function Get-StopState {
    $p = Join-Path $TeamFolder 'stop.md'
    if (-not (Test-Path -LiteralPath $p)) { return $null }
    $s = @{ Mode = 'now'; Since = ''; By = ''; Reason = '' }
    foreach ($l in (Read-Lines $p)) {
        if ($l -match '^mode:\s*(\S+)') { if ($Matches[1] -eq 'finish') { $s.Mode = 'finish' } }
        elseif ($l -match '^since:\s*(.*)$') { $s.Since = $Matches[1].Trim() }
        elseif ($l -match '^by:\s*(.*)$') { $s.By = $Matches[1].Trim() }
        elseif ($l -match '^reason:\s*(.*)$') { $s.Reason = $Matches[1].Trim() }
    }
    return [pscustomobject]$s
}

function Invoke-Stop([string]$Mode, [string]$Reason, [string]$Who) {
    $now = Get-Date -Format 'yyyy-MM-dd HH:mm'
    if (-not $Reason) { $Reason = 'asked by the user' }
    $what = @{
        finish = "Team stop: start no new work. Finish the task you are working on now (its current round): build, test, format, commit and push it as RULES.md says, set it to Done, release your locks, and end your run. Take no other task. The orchestrator then has the work reviewed, asks the user to approve the pull requests, and merges them."
        now    = "Emergency stop: stop at once. Do not commit, push, merge, or run any other command. Write one line under ## Progress in your task (what is done, what is not, which files are changed and not committed), set the task back to Open, release your locks (-Unlock all -Owner <your instance>), and end your run with the report 'Stopped: emergency stop'. Leave changed files as they are."
    }
    $text = "# Team stop`n`nmode: $Mode`nsince: $now`nby: $Who`nreason: $(($Reason -replace '\s+', ' ').Trim())`n`n$($what[$Mode])`n`nWritten by the team script (-Stop $Mode). Only the orchestrator ends it, with -Resume (team continue).`n"
    Write-TextFile (Join-Path $TeamFolder 'stop.md') $text
    Write-Output ("Stopped the team ({0}) at {1}: stop.md written" -f $Mode, $now)
}

function Invoke-Resume {
    $p = Join-Path $TeamFolder 'stop.md'
    if (-not (Test-Path -LiteralPath $p)) { Write-Output 'The team was not stopped (no stop.md).'; return }
    Remove-Item -LiteralPath $p -Force
    Write-Output 'Resumed: stop.md removed'
}

# --- Routine edits of the orchestrator -------------------------------------------------------

# The item file of an id, or $null.
function Get-ItemPath([string]$Id) {
    foreach ($k in $kinds.Keys) {
        if ($Id -match ('^' + $kinds[$k].Prefix + '-\d+$')) { return @{ Kind = $k; Path = (Join-Path (Join-Path $TeamFolder $kinds[$k].Folder) "$Id.md") } }
    }
    return $null
}

# True when someone other than $Who holds the lock on $Id (then the file is left alone).
function Test-LockedByOther([string]$Id, [string]$Who) {
    $path = Join-Path (Join-Path $TeamFolder 'locks') "$Id.lock"
    if (-not (Test-Path -LiteralPath $path)) { return $false }
    $held = Read-Lock $path
    if ($held.Owner -eq $Who) { return $false }
    if (((Get-Date) - $held.Time).TotalMinutes -ge $LockMinutes) { return $false }
    # Written to the host, so the message is not mixed into the return value.
    [Console]::Out.WriteLine(("LOCKED   {0} is locked by {1}: not changed; try again later" -f $Id, $held.Owner))
    return $true
}

# Sets "key: value" in the header of an item file (adds the key when it is missing).
function Set-HeaderField([string]$Path, [string]$Key, [string]$Value) {
    $text = [System.IO.File]::ReadAllText($Path) -replace "`r`n", "`n"
    $line = ('{0}: {1}' -f $Key, $Value).TrimEnd()
    if ($Value -eq '') { $line = "${Key}: " }
    $existing = New-Object System.Text.RegularExpressions.Regex ('(?m)^' + [regex]::Escape($Key) + ':[^\n]*$')
    if ($existing.IsMatch($text)) {
        $text = $existing.Replace($text, $line.Replace('$', '$$'), 1)
    } else {
        $header = New-Object System.Text.RegularExpressions.Regex '(?s)^(---\n.*?)(\n---\n)'
        $replacement = '$1' + "`n" + $line.Replace('$', '$$') + '$2'
        $text = $header.Replace($text, $replacement, 1)
    }
    Write-TextFile $Path $text
}

function Invoke-SetStatus([string]$Changes, [string]$Who) {
    foreach ($pair in @($Changes -split '\s*,\s*' | Where-Object { $_ })) {
        if ($pair -notmatch '^\s*([A-Za-z]+-\d+)\s*=\s*(.+?)\s*$') { Write-Output "SKIPPED  '$pair': write it as ID=Status"; continue }
        $id = $Matches[1].ToUpper(); $status = $Matches[2]
        $item = Get-ItemPath $id
        if (-not $item -or -not (Test-Path -LiteralPath $item.Path)) { Write-Output "SKIPPED  $id does not exist"; continue }
        $ok = $kinds[$item.Kind].Statuses -contains $status
        if ($item.Kind -eq 'decision' -and $status -match '^Superseded by D-\d+$') { $ok = $true }
        if (-not $ok) { Write-Output ("SKIPPED  {0}: '{1}' is not one of: {2}" -f $id, $status, ($kinds[$item.Kind].Statuses -join ', ')); continue }
        if (Test-LockedByOther $id $Who) { continue }
        Set-HeaderField $item.Path 'status' $status
        Write-Output "Set $id to $status"
    }
}

# A new round of work in an existing task (one task per item and role): the round goes under
# "## Task"; a Done task is Open again, and its merged: is cleared, so its new commits get a new
# pull request and a new review.
function Invoke-Reopen([string]$Id, [string]$RoundText, [string]$Who) {
    $Id = $Id.Trim().ToUpper()
    $item = Get-ItemPath $Id
    if (-not $item -or $item.Kind -ne 'task' -or -not (Test-Path -LiteralPath $item.Path)) { throw "$Id is not an existing task." }
    if (-not $RoundText) { throw 'Give the work of the new round with -Text.' }
    if (Test-LockedByOther $Id $Who) { $script:exitCode = 2; return }
    $t = Read-Item $item.Path 'task'
    if (-not $t.HasHeader) { throw "$Id has no header between two '---' lines: restore it first (-Restore $Id)." }
    $text = [System.IO.File]::ReadAllText($item.Path) -replace "`r`n", "`n"
    $round = 2
    foreach ($m in [regex]::Matches($text, '(?m)^### Round (\d+)\b')) { $n = [int]$m.Groups[1].Value; if ($n -ge $round) { $round = $n + 1 } }
    $merged = Get-Field $t 'merged'
    $note = ''
    if ($merged) { $note = " (round $($round - 1) merged $merged)" }
    $entry = "### Round $round, $(Get-Date -Format 'yyyy-MM-dd')$note`n`n$($RoundText.Trim())`n"
    $plan = [regex]::Match($text, '(?m)^## Plan\s*$')
    if ($plan.Success) { $text = $text.Insert($plan.Index, $entry + "`n") }
    else {
        $sec = [regex]::Match($text, '(?ms)^## Task\s*\n.*?(?=^## |\z)')
        if ($sec.Success) { $text = $text.Insert($sec.Index + $sec.Length, $entry + "`n") } else { $text = $text.TrimEnd() + "`n`n## Task`n`n" + $entry }
    }
    Write-TextFile $item.Path $text
    $status = Get-Field $t 'status'
    if ($status -eq 'Done') { Set-HeaderField $item.Path 'status' 'Open'; $status = 'Open' }
    if ($merged) { Set-HeaderField $item.Path 'merged' '' }
    Write-Output ("Reopened {0}: round {1} added under ## Task, status {2}" -f $Id, $round, $status)
}

function Invoke-Answer([string]$Id, [string]$Answer, [string]$Who) {
    $item = Get-ItemPath $Id.ToUpper()
    if (-not $item -or $item.Kind -ne 'question' -or -not (Test-Path -LiteralPath $item.Path)) { throw "$Id is not an existing question." }
    if (-not $Answer) { throw 'Give the answer with -Text.' }
    if (Test-LockedByOther $Id.ToUpper() $Who) { $script:exitCode = 2; return }
    $q = Read-Item $item.Path 'question'
    $by = $Who
    if ((Get-Field $q 'to') -eq 'user' -and $Who -eq 'orchestrator') { $by = 'user (recorded by the orchestrator)' }
    $entry = '**{0}, {1}:** {2}' -f $by, (Get-Date -Format 'yyyy-MM-dd HH:mm'), (($Answer -replace '\s+', ' ').Trim())
    $text = [System.IO.File]::ReadAllText($item.Path) -replace "`r`n", "`n"
    # Drop the placeholder of a new question, then add the answer at the end of "## Answer".
    $text = [regex]::Replace($text, '(?m)^\(Written by the addressee[^\n]*\)\n?', '')
    if ($text -match '(?m)^## Answer\s*$') {
        $m = [regex]::Match($text, '(?ms)^## Answer\s*\n(.*?)(?=^## |\z)')
        $before = $text.Substring(0, $m.Index + $m.Length).TrimEnd()
        $after = $text.Substring($m.Index + $m.Length)
        $text = $before + "`n`n" + $entry + "`n"
        if ($after.Trim()) { $text += "`n" + $after.TrimStart() }
    } else { $text = $text.TrimEnd() + "`n`n## Answer`n`n" + $entry + "`n" }
    Write-TextFile $item.Path $text
    Set-HeaderField $item.Path 'status' 'Closed'
    Write-Output "Answered and closed $($Id.ToUpper())"
    Invoke-Unblock $Who
}

# Blocked tasks whose waits are all finished (or archived, or removed on purpose) go back to Open.
function Invoke-Unblock([string]$Who) {
    $items = @(Get-Items $TeamFolder)
    $byId = @{}
    foreach ($it in $items) { if ($it.HasHeader) { $byId[$it.File] = $it } }
    $gone = Get-GoneOnPurpose
    $count = 0
    foreach ($t in @($items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'status') -eq 'Blocked' })) {
        $w = Get-ItemWaits (Get-Field $t 'waits-for')
        if ($w.User) { continue }
        $refs = @(@($w.Tasks) + @($w.Questions) + @($w.Decisions))
        if ($refs.Count -eq 0) { continue }
        $pending = @($refs | Where-Object {
                if ($gone.ContainsKey($_)) { $false }
                elseif (-not $byId.ContainsKey($_)) { $true }
                else {
                    $s = Get-Field $byId[$_] 'status'
                    switch ($byId[$_].Kind) { 'task' { $s -ne 'Done' } 'question' { $s -eq 'Open' } 'decision' { $s -eq 'Proposed' } default { $false } }
                }
            }).Count
        if ($pending -gt 0) { continue }
        if (Test-LockedByOther $t.File $Who) { continue }
        Set-HeaderField $t.Path 'status' 'Open'
        Set-HeaderField $t.Path 'waits-for' ''
        Write-Output ("Unblocked {0}: everything it waited for ({1}) is finished" -f $t.File, ($refs -join ', '))
        $count++
    }
    if ($count -eq 0) { Write-Output 'No task to unblock.' }
}

# --- Compaction ------------------------------------------------------------------------------

# Moves Done tasks and Closed questions to archive/done/, except those an unfinished task waits
# for and those somebody has locked.
function Invoke-Compact($Items) {
    $waitedFor = @{}
    foreach ($t in @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader -and (Get-Field $_ 'status') -ne 'Done' })) {
        $w = Get-ItemWaits (Get-Field $t 'waits-for')
        foreach ($ref in (@($w.Tasks) + @($w.Questions))) { $waitedFor[$ref] = $true }
    }
    $locked = @{}
    foreach ($l in (Get-Locks)) { $locked[$l.Name] = $true }
    $moved = New-Object 'System.Collections.Generic.List[string]'
    $kept = New-Object 'System.Collections.Generic.List[string]'
    foreach ($it in $Items) {
        if (-not $it.HasHeader) { continue }
        $id = Get-Field $it 'id'
        $status = Get-Field $it 'status'
        $finished = ($it.Kind -eq 'task' -and $status -eq 'Done') -or ($it.Kind -eq 'question' -and $status -eq 'Closed')
        if (-not $finished) { continue }
        if ($waitedFor.ContainsKey($id) -or $locked.ContainsKey($id)) { $kept.Add($id); continue }
        $target = Join-Path (Join-Path (Join-Path $TeamFolder 'archive') 'done') $kinds[$it.Kind].Folder
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        Move-Item -LiteralPath $it.Path -Destination $target
        $moved.Add($id)
    }
    Write-Output ('Compacted: moved {0} finished items to archive/done/: {1}' -f $moved.Count, (Join-OrDash $moved))
    if ($kept.Count -gt 0) { Write-Output ('Kept (still waited for, or locked): {0}' -f ($kept -join ', ')) }
}

# --- Project manager report ------------------------------------------------------------------

# The rows of the first table below a "## <Section>" heading (cells per row, header and separator left out).
function Get-SectionRows([string]$Path, [string]$Section) {
    $rows = New-Object 'System.Collections.Generic.List[object]'
    if (-not (Test-Path -LiteralPath $Path)) { return $rows }
    $in = $false; $seenHeader = $false
    foreach ($l in (Read-Lines $Path)) {
        if ($l -match '^##\s+') { if ($in) { break }; $in = ($l -match ('^##\s+' + [regex]::Escape($Section))); continue }
        if (-not $in -or -not $l.TrimStart().StartsWith('|')) { continue }
        if ($l -match '^\s*\|[\s:|-]+\|\s*$') { continue }
        if (-not $seenHeader) { $seenHeader = $true; continue }
        $rows.Add(@(Get-Cells $l))
    }
    return $rows
}

# The entry of a review file with the newest date (lines that start with a bold date), whatever
# the order of the file.
function Get-LatestEntry([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $best = ''; $bestDate = ''
    foreach ($l in (Read-Lines $Path)) {
        if ($l -match '^\*\*(\d{4}-\d{2}-\d{2})' -and $Matches[1] -ge $bestDate) { $bestDate = $Matches[1]; $best = $l }
    }
    return (($best -replace '\*\*', '') -replace '\s+', ' ').Trim()
}

function Get-Points([string]$Text) {
    if ($Text -match '\((\d+(?:[.,]\d+)?)\s*SP\)') { return [double]($Matches[1] -replace ',', '.') }
    return 0
}

function Build-Report($Items, $Team) {
    $projectFile = Join-Path $TeamFolder 'project.md'
    $title = 'Project'; $boundaries = New-Object 'System.Collections.Generic.List[string]'; $timeUsed = ''
    if (Test-Path -LiteralPath $projectFile) {
        foreach ($l in (Read-Lines $projectFile)) {
            if ($l -match '^#\s+(?:Project:\s*)?(.+)$' -and $title -eq 'Project') { $title = $Matches[1].Trim() }
            if ($l -match '^\|\s*(Time|Resources|Budget)\b[^|]*\|\s*([^|]*)\|') { $boundaries.Add(('{0}: {1}' -f $Matches[1], $Matches[2].Trim())) }
            if ($l -match '^\*\*Time used:\*\*\s*(.+)$') { $timeUsed = $Matches[1].Trim() }
        }
    }
    $by = @{}
    foreach ($k in $kinds.Keys) { $by[$k] = @($Items | Where-Object { $_.Kind -eq $k -and $_.HasHeader }) }
    $archived = Get-ArchivedDone
    $doneArchived = @($archived.Keys | Where-Object { $_ -like 'T-*' }).Count
    $closedArchived = @($archived.Keys | Where-Object { $_ -like 'Q-*' }).Count
    $count = { param($list, $status) @($list | Where-Object { (Get-Field $_ 'status') -eq $status }).Count }

    $reqApproved = & $count $by.requirement 'Approved'; $reqProposed = & $count $by.requirement 'Proposed'; $reqWont = & $count $by.requirement "Won't"
    $qOpen = @($by.question | Where-Object { (Get-Field $_ 'status') -eq 'Open' })
    $qUser = @($qOpen | Where-Object { (Get-Field $_ 'to') -eq 'user' })
    $qClosed = (& $count $by.question 'Closed') + $closedArchived
    $dAccepted = & $count $by.decision 'Accepted'; $dProposed = & $count $by.decision 'Proposed'
    $dRejected = & $count $by.decision 'Rejected'; $dSuperseded = @($by.decision | Where-Object { (Get-Field $_ 'status') -like 'Superseded*' }).Count
    $tDone = (& $count $by.task 'Done') + $doneArchived; $tOpen = & $count $by.task 'Open'
    $tWorking = & $count $by.task 'In progress'; $tBlocked = & $count $by.task 'Blocked'
    $tTotal = $tDone + $tOpen + $tWorking + $tBlocked
    $spDone = 0; $spLeft = 0
    foreach ($t in $by.task) { $p = Get-Points (Get-Field $t 'title'); if ((Get-Field $t 'status') -eq 'Done') { $spDone += $p } else { $spLeft += $p } }
    $pct = { param($a, $b) if ($b -gt 0) { [int][Math]::Round(100 * $a / $b) } else { 0 } }

    $runs = @(Get-SectionRows (Join-Path $TeamFolder 'testing.md') 'Test runs')
    $bugs = @(Get-SectionRows (Join-Path $TeamFolder 'testing.md') 'Bugs')
    $openBugs = @($bugs | Where-Object { ($_ -join '|') -match '\bOpen\b' })
    $severe = @($openBugs | Where-Object { ($_ -join '|') -match '\b(Critical|High)\b' })
    $security = Get-LatestEntry (Join-Path (Join-Path $TeamFolder 'reviews') 'security-assessments.md')
    $sanity = Get-LatestEntry (Join-Path (Join-Path $TeamFolder 'reviews') 'sanity-reviews.md')
    $codeReview = $null
    $codeReviewFile = Join-Path (Join-Path $TeamFolder 'reviews') 'code-reviews.md'
    if (Test-Path -LiteralPath $codeReviewFile) { $codeReview = Get-LatestEntry $codeReviewFile }

    $o = New-Object 'System.Collections.Generic.List[string]'
    $o.Add("# Project report: $title")
    $o.Add('')
    $o.Add(('_Generated by check-decisions.ps1 on {0} for the project manager. Do not edit this file. The details per item are in decisions.md._' -f (Get-Date -Format 'yyyy-MM-dd HH:mm')))
    $o.Add('')
    if ($boundaries.Count -gt 0) { $o.Add('**Boundaries:** ' + ($boundaries -join '; ')); $o.Add('') }
    $o.Add('## Milestones')
    $o.Add('')
    $mark = { param($ok) if ($ok) { '[x]' } else { '[ ]' } }
    $o.Add(('- {0} Requirements approved: {1} approved, {2} waiting for the user, {3} won''t' -f (& $mark ($reqApproved -gt 0 -and $reqProposed -eq 0)), $reqApproved, $reqProposed, $reqWont))
    $o.Add(('- {0} Decisions settled: {1} accepted, {2} waiting' -f (& $mark ($dProposed -eq 0 -and $dAccepted -gt 0)), $dAccepted, $dProposed))
    $o.Add(('- {0} Questions answered: {1} answered, {2} open ({3} for the user)' -f (& $mark ($qOpen.Count -eq 0)), $qClosed, $qOpen.Count, $qUser.Count))
    $o.Add(('- {0} Work done: {1} of {2} tasks ({3}%)' -f (& $mark ($tTotal -gt 0 -and $tDone -eq $tTotal)), $tDone, $tTotal, (& $pct $tDone $tTotal)))
    if ($runs.Count -gt 0) { $last = ($runs[$runs.Count - 1] | Select-Object -First 4) -join ', '; $o.Add(('- [x] Tests run: {0} runs, latest: {1}' -f $runs.Count, $last)) } else { $o.Add('- [ ] Tests run: none recorded') }
    $o.Add(('- {0} No open Critical or High bugs: {1} open bugs, {2} of them Critical or High' -f (& $mark ($runs.Count -gt 0 -and $severe.Count -eq 0)), $openBugs.Count, $severe.Count))
    if ($security) { $o.Add("- Security, latest: $security") } else { $o.Add('- [ ] Security assessment: none recorded') }
    if ($sanity) { $o.Add("- Customer check, latest: $sanity") }
    if ($codeReview) { $o.Add("- Code review, latest: $codeReview") }
    $o.Add('')
    # The delivery plan: per sprint its goal, requirements, progress and status.
    $si = $script:sprintInfo
    $o.Add('## Sprints')
    $o.Add('')
    if (-not $si -or -not $si.HasPlan) {
        $len = '-'; if ($si -and $si.Length) { $len = $si.Length }
        $o.Add(('- No delivery plan yet (plan.md). Sprint length: {0}.' -f $len))
    }
    else {
        $appr = 'not yet'; if ($si.Approved) { $appr = $si.Approved }
        $len = 'not recorded'; if ($si.Length) { $len = $si.Length }
        $o.Add(('Sprint length: {0}. Plan approved by the user: {1}.' -f $len, $appr))
        $o.Add('')
        $o.Add('| Sprint | Goal: what you can see or do | Requirements | Tasks done | Status | Demo |')
        $o.Add('|---|---|---|---|---|---|')
        foreach ($s in $si.Sprints) {
            $mineTasks = @($by.task | Where-Object { $f = Get-Field $_ 'for'; $f -and $f -ne 'team' -and @(@(Get-ItemRequirements $f $Items) | Where-Object { $s.Requirements -contains $_ }).Count -gt 0 })
            $doneTasks = @($mineTasks | Where-Object { (Get-Field $_ 'status') -eq 'Done' }).Count
            $o.Add(('| {0} | {1} | {2} | {3} of {4} | {5} | {6} |' -f $s.Name, (Format-Cell $s.Goal), (Join-OrDash $s.Requirements), $doneTasks, $mineTasks.Count, $s.Status, (Format-Cell $s.Demo)))
        }
    }
    $o.Add('')
    $o.Add('## Waiting for the user')
    $o.Add('')
    $w = 0
    foreach ($q in $qUser) { $o.Add(('- {0}: {1}' -f (Get-Field $q 'id'), (Get-Field $q 'title'))); $w++ }
    $wait = @($by.requirement | Where-Object { (Get-Field $_ 'status') -eq 'Proposed' })
    if ($wait.Count -gt 0) { $o.Add('- Approve the requirements: ' + (($wait | ForEach-Object { '{0} ({1})' -f (Get-Field $_ 'id'), (Get-ShortTitle (Get-Field $_ 'title')) }) -join '; ')); $w++ }
    foreach ($u in $script:userSteps) { $o.Add(('- {0}{1}.' -f $u.Substring(0, 1).ToUpper(), $u.Substring(1))); $w++ }
    foreach ($p in $script:readyPullRequests) { $o.Add("- Review on GitHub and approve in the team's chat: $p."); $w++ }
    if ($w -eq 0) { $o.Add('- Nothing.') }
    $o.Add('')
    $o.Add('## Work')
    $o.Add('')
    $o.Add(('{0} tasks: {1} done, {2} in progress, {3} open, {4} blocked. Story points (where tasks give them): {5} done, {6} left.' -f $tTotal, $tDone, $tWorking, $tOpen, $tBlocked, $spDone, $spLeft))
    $o.Add('')
    $o.Add('| Role | Done | In progress | Open | Blocked | Waits for |')
    $o.Add('|---|---|---|---|---|---|')
    foreach ($r in $Team.Rows) { $o.Add(('| {0} | {1} | {2} | {3} | {4} | {5} |' -f $r.Name, $r.Done, $r.Working, $r.Open, $r.Blocked, (Join-OrDash $r.BlockedBy))) }
    $o.Add('')
    # Per item (requirement, bug or request): its branch, pull request and tasks.
    $forItems = [ordered]@{}
    foreach ($t in $by.task) { $f = Get-Field $t 'for'; if ($f) { if (-not $forItems.Contains($f)) { $forItems[$f] = @() }; $forItems[$f] += $t } }
    foreach ($it in @($by.requirement) + @($by.decision)) { if ((Get-Field $it 'pr') -or (Get-Field $it 'branch') -or (Get-Field $it 'worktree')) { $id = Get-Field $it 'id'; if (-not $forItems.Contains($id)) { $forItems[$id] = @() } } }
    if ($forItems.Count -gt 0) {
        $o.Add('## Pull requests')
        $o.Add('')
        $o.Add('| Item | Title | Branch | Folder | Pull request | Tasks done |')
        $o.Add('|---|---|---|---|---|---|')
        foreach ($id in $forItems.Keys) {
            $item = @($Items | Where-Object { $_.HasHeader -and $_.File -eq $id } | Select-Object -First 1)
            $itTitle = '-'; $branch = '-'; $pr = '-'; $folder = 'main folder'
            if ($item.Count -gt 0) { $itTitle = Format-Cell (Get-ShortTitle (Get-Field $item[0] 'title')); $branch = Format-Cell (Get-Field $item[0] 'branch'); $pr = Format-Cell (Get-Field $item[0] 'pr'); if (Get-Field $item[0] 'worktree') { $folder = Get-Field $item[0] 'worktree' } }
            $list = @($forItems[$id])
            $o.Add(('| {0} | {1} | {2} | {3} | {4} | {5} of {6} |' -f $id, $itTitle, $branch, $folder, $pr, @($list | Where-Object { (Get-Field $_ 'status') -eq 'Done' }).Count, $list.Count))
        }
        $o.Add('')
    }
    # Scope and time: active time against the boundary, what was added per day, and the scope review.
    $o.Add('## Scope and time')
    $o.Add('')
    $sc = $script:scope
    if ($sc -and $sc.Active) {
        $days = (@($sc.Active.PerDay.Keys | ForEach-Object { '{0}: {1} h' -f $_, [Math]::Round($sc.Active.PerDay[$_] / 60, 1) }) -join '; ')
        if ($sc.BoundaryHours -gt 0) { $o.Add(('- **Active time:** about {0} of {1} hours ({2}%), estimated from the team''s activity (issued ids, commits, checked copies). Per day: {3}.' -f $sc.Active.Hours, $sc.BoundaryHours, (& $pct $sc.Active.Hours $sc.BoundaryHours), $days)) }
        else { $o.Add(('- **Active time:** about {0} hours, estimated from the team''s activity; no time boundary in hours in project.md. Per day: {1}.' -f $sc.Active.Hours, $days)) }
    }
    $added = [ordered]@{}
    foreach ($e in (Get-IssuedIds).GetEnumerator()) {
        if ($e.Key -notmatch '^(T|Q|D|REQ)-' -or $e.Value.Time -notmatch '^(\d{4}-\d{2}-\d{2})') { continue }
        $day = $Matches[1]; $k = ($e.Key -replace '-\d+$', '')
        if (-not $added.Contains($day)) { $added[$day] = @{ T = 0; Q = 0; D = 0; REQ = 0 } }
        $added[$day][$k]++
    }
    if ($added.Count -gt 0) { $o.Add('- **Added per day** (tasks, questions, decisions, requirements): ' + ((@($added.Keys | ForEach-Object { '{0}: {1} T, {2} Q, {3} D, {4} REQ' -f $_, $added[$_].T, $added[$_].Q, $added[$_].D, $added[$_].REQ })) -join '; ') + '.') }
    $o.Add(('- **Open work:** {0} tasks ({1} story points where given).' -f ($tOpen + $tWorking + $tBlocked), $spLeft))
    if ($sc) {
        if ($sc.Outside.Count -gt 0) { $o.Add('- **Outside the approved requirements** (nobody works on them before they are linked to a requirement or the user agreed): ' + ($sc.Outside -join ', ') + '.') }
        else { $o.Add('- **Outside the approved requirements:** none.') }
        $last = 'none yet'; if ($sanity) { $last = $sanity }
        $upto = ''; if ($sc.ReviewedUpTo) { $upto = " (reviewed up to $($sc.ReviewedUpTo))" }
        $o.Add(('- **Scope review** by the sanity agent, latest: {0}{1}. New since: {2} tasks, {3} decisions, {4} requirements.' -f $last, $upto, $sc.NewTasks, $sc.NewDecisions, $sc.NewRequirements))
    }
    $o.Add('')
    $o.Add('## Risks')
    $o.Add('')
    $risks = 0
    if ($sc -and $sc.Active -and $sc.BoundaryHours -gt 0) {
        if ($sc.Active.Hours -ge 0.8 * $sc.BoundaryHours) { $o.Add(('- The team has used about {0} of its {1} hours: decide with the team whether to go on, cut scope or move the boundary.' -f $sc.Active.Hours, $sc.BoundaryHours)); $risks++ }
    }
    elseif ($timeUsed) { $o.Add("- Time used: $timeUsed (boundary: $(@($boundaries | Where-Object { $_ -like 'Time:*' }) -join ''))."); $risks++ }
    if ($sc -and $sc.Outside.Count -gt 0) { $o.Add(('- {0} tasks or decisions are outside the approved requirements (see Scope and time).' -f $sc.Outside.Count)); $risks++ }
    if ($runs.Count -eq 0) { $o.Add('- No test run is recorded: the quality of what is built is unknown.'); $risks++ }
    if ($tBlocked -gt 0) { $o.Add(("- {0} tasks are blocked; a delay at the start of their chain delays them all." -f $tBlocked)); $risks++ }
    if ($severe.Count -gt 0) { $o.Add(("- {0} open Critical or High bugs." -f $severe.Count)); $risks++ }
    if ($script:errors.Count -gt 0) { $o.Add(("- The team folder has {0} errors (see decisions.md, Problems)." -f $script:errors.Count)); $risks++ }
    if ($qUser.Count + $wait.Count -gt 0) { $o.Add('- Work waits for the user (see above).'); $risks++ }
    if ($risks -eq 0) { $o.Add('- None found.') }
    $o.Add('')
    $target = Join-Path $TeamFolder 'report.md'
    $temp = "$target.$PID.tmp"
    Write-TextFile $temp (($o -join "`n") + "`n")
    Move-Item -LiteralPath $temp -Destination $target -Force
    return $o
}

# The daily standup (standup.md): the sprint, what waits for the user, the blockers, per role the
# tasks of today, and what was done since the last working day.
function Build-Standup($Items, $Team) {
    $now = Get-Date
    $since = $now.Date.AddDays(-1)
    if ($now.DayOfWeek -eq [DayOfWeek]::Monday) { $since = $now.Date.AddDays(-3) }
    $tasks = @($Items | Where-Object { $_.Kind -eq 'task' -and $_.HasHeader })
    $byId = @{}
    foreach ($t in $tasks) { $byId[(Get-Field $t 'id')] = $t }
    $openQ = @($Items | Where-Object { $_.Kind -eq 'question' -and $_.HasHeader -and (Get-Field $_ 'status') -eq 'Open' })
    $label = {
        param($t)
        $f = Get-Field $t 'for'; $tk = Get-Field $t 'ticket'; $p = Get-Points (Get-Field $t 'title')
        $parts = @((Get-Field $t 'id'))
        $what = @($f, $tk) | Where-Object { $_ }
        if ($what.Count -gt 0) { $parts += '(' + ($what -join ', ') + ')' }
        $text = ($parts -join ' ') + ' ' + (Get-ShortTitle (Get-Field $t 'title'))
        if ($p -gt 0) { $text += " ($p SP)" }
        return $text
    }
    # How many blocked tasks wait for each task: the ones that unblock most go first today.
    $unblocks = @{}
    foreach ($t in @($tasks | Where-Object { (Get-Field $_ 'status') -eq 'Blocked' })) {
        foreach ($n in (Get-ItemWaits (Get-Field $t 'waits-for')).Tasks) { $unblocks[$n] = 1 + [int]$unblocks[$n] }
    }
    $repoRoot = Split-Path (Resolve-Path -LiteralPath $TeamFolder).Path -Parent
    $commits = @{}
    foreach ($s in @(Get-GitLines $repoRoot @('log', '--all', ('--since=' + $since.ToString('yyyy-MM-dd HH:mm')), '--format=%s'))) {
        if ($s -match '^(T-\d+)\b') { $commits[$Matches[1]] = 1 + [int]$commits[$Matches[1]] }
    }

    $o = New-Object 'System.Collections.Generic.List[string]'
    $o.Add(('# Daily standup: {0}' -f $now.ToString('yyyy-MM-dd (dddd)', [System.Globalization.CultureInfo]::InvariantCulture)))
    $o.Add('')
    $o.Add(('_Generated by check-decisions.ps1 at {0}. Do not edit this file. Done since {1}._' -f $now.ToString('HH:mm'), $since.ToString('yyyy-MM-dd')))
    $o.Add('')
    $si = $script:sprintInfo
    if ($si -and $si.Current) {
        $c = $si.Current
        $o.Add(('**Sprint {0}** ({1}, status {2}): {3}. Demo: {4}' -f $c.Name, $c.Dates, $c.Status, $c.Goal, $c.Demo))
    }
    elseif ($si -and $si.HasPlan) { $o.Add('**Sprint:** no sprint is Current in plan.md.') }
    else { $o.Add('**Sprint:** no delivery plan yet (plan.md).') }
    $o.Add('')

    $o.Add('## Waiting for you')
    $o.Add('')
    $w = 0
    foreach ($q in @($openQ | Where-Object { (Get-Field $_ 'to') -eq 'user' })) { $o.Add(('- {0}: {1}' -f (Get-Field $q 'id'), (Get-Field $q 'title'))); $w++ }
    foreach ($u in $script:userSteps) { $o.Add(('- {0}{1}.' -f $u.Substring(0, 1).ToUpper(), $u.Substring(1))); $w++ }
    foreach ($p in $script:readyPullRequests) { $o.Add("- Review on GitHub and approve in the team's chat: $p."); $w++ }
    if ($w -eq 0) { $o.Add('- Nothing.') }
    $o.Add('')

    $o.Add('## Blockers')
    $o.Add('')
    $b = 0
    foreach ($t in @($tasks | Where-Object { (Get-Field $_ 'status') -eq 'Blocked' } | Sort-Object { Get-IdNumber $_.File })) {
        if ($script:notNow.ContainsKey((Get-Field $t 'id'))) { continue }
        $waits = Get-ItemWaits (Get-Field $t 'waits-for')
        $on = New-Object 'System.Collections.Generic.List[string]'
        foreach ($n in $waits.Tasks) { if ($byId.ContainsKey($n) -and (Get-Field $byId[$n] 'status') -ne 'Done') { $on.Add(('{0} ({1}, {2})' -f $n, (Get-Field $byId[$n] 'owner'), (Get-Field $byId[$n] 'status'))) } }
        foreach ($n in $waits.Questions) { $q = @($openQ | Where-Object { (Get-Field $_ 'id') -eq $n }); if ($q.Count -gt 0) { $on.Add(('{0} (question to {1})' -f $n, (Get-Field $q[0] 'to'))) } }
        if ($waits.User) { $on.Add('the user') }
        if ($on.Count -eq 0) { continue }
        $o.Add(('- {0}, {1}: waits for {2}' -f (& $label $t), (Get-Field $t 'owner'), ($on -join ', ')))
        $b++
    }
    $parked = @($script:notNow.Keys | Sort-Object { Get-IdNumber $_ })
    if ($parked.Count -gt 0) { $o.Add(('- Not to start now (outside the requirements or the current sprint): {0}' -f ($parked -join ', '))); $b++ }
    if ($script:errors.Count -gt 0) { $o.Add(('- The team folder has {0} error(s): the orchestrator repairs them first (see decisions.md, Problems).' -f $script:errors.Count)); $b++ }
    $stale = @($script:warnings | Where-Object { $_ -match '(?i)stale|deadlock|no sign of life' })
    foreach ($s in $stale) { $o.Add('- ' + (($s -replace '^WARNING\s+', '') -replace '\s+', ' ')); $b++ }
    if ($b -eq 0) { $o.Add('- None.') }
    $o.Add('')

    $o.Add('## Today, per role')
    $o.Add('')
    $o.Add('| Role | In progress | Next today (the first unblocks most) | Questions to answer |')
    $o.Add('|---|---|---|---|')
    $roles = @($Team.Rows | ForEach-Object { $_.Name })
    foreach ($name in $roles) {
        $mine = @($tasks | Where-Object { (Get-Field $_ 'owner') -eq $name })
        $working = @($mine | Where-Object { (Get-Field $_ 'status') -eq 'In progress' } | ForEach-Object { & $label $_ })
        $next = @($mine | Where-Object { (Get-Field $_ 'status') -eq 'Open' -and -not $script:notNow.ContainsKey((Get-Field $_ 'id')) } |
                Sort-Object @{ Expression = { [int]$unblocks[(Get-Field $_ 'id')] }; Descending = $true }, @{ Expression = { Get-IdNumber $_.File } } |
                ForEach-Object { $u = [int]$unblocks[(Get-Field $_ 'id')]; $x = & $label $_; if ($u -gt 0) { $x += " - unblocks $u" }; $x })
        $row = @($Team.Rows | Where-Object { $_.Name -eq $name })[0]
        if ($working.Count + $next.Count + $row.Must.Count -eq 0) { continue }
        $o.Add(('| {0} | {1} | {2} | {3} |' -f $name, (Format-Cell (Join-OrDash $working)), (Format-Cell (Join-OrDash $next)), (Join-OrDash $row.Must)))
    }
    $o.Add('')

    $o.Add(('## Done since {0}' -f $since.ToString('yyyy-MM-dd')))
    $o.Add('')
    $done = @($tasks | Where-Object { (Get-Field $_ 'status') -eq 'Done' -and (Get-Item -LiteralPath $_.Path).LastWriteTime -ge $since } | Sort-Object { Get-IdNumber $_.File })
    if ($done.Count -eq 0) { $o.Add('- Nothing finished.') }
    foreach ($t in $done) {
        $id = Get-Field $t 'id'
        $c = ''; if ($commits.ContainsKey($id)) { $c = (' - {0} commit(s)' -f $commits[$id]) }
        $o.Add(('- {0}, {1}{2}' -f (& $label $t), (Get-Field $t 'owner'), $c))
    }
    $o.Add('')
    $target = Join-Path $TeamFolder 'standup.md'
    $temp = "$target.$PID.tmp"
    Write-TextFile $temp (($o -join "`n") + "`n")
    Move-Item -LiteralPath $temp -Destination $target -Force
    return $o
}

# --- Main ------------------------------------------------------------------------------------

if (-not (Test-Path -LiteralPath $TeamFolder)) { Write-Output "ERROR    $TeamFolder does not exist"; exit 1 }

$stopWho = $Owner; if (-not $stopWho) { $stopWho = 'orchestrator' }
if ($Stop) { Invoke-Stop $Stop $Text $stopWho; $Build = $true }
if ($Resume) { Invoke-Resume; $Build = $true }
$script:stopState = Get-StopState
if ($script:stopState) {
    $st = $script:stopState
    if ($st.Mode -eq 'now') {
        Write-Output ("EMERGENCY STOP since {0} ({1}: {2}): stop at once, see stop.md. No commit, push, merge or other command; set your task back to Open, release your locks, end your run." -f $st.Since, $st.By, $st.Reason)
        if ($New -or $Reopen -or $Lock) { Write-Output 'REFUSED: nothing changed during an emergency stop. Only the orchestrator ends it, with -Resume (team continue).'; exit 2 }
    }
    else { Write-Output ("TEAM STOP since {0} ({1}: {2}): start no new work; finish your current task, commit and push it, then end your run. See stop.md." -f $st.Since, $st.By, $st.Reason) }
}

if ($New) { New-TeamItem; exit 0 }
# A lock or unlock call can also check and build in the same call (-Unlock all -Owner x -Build).
if ($Lock) { Lock-TeamFiles $Lock $Owner; if ($script:exitCode -ne 0 -or -not ($Build -or $Overview)) { exit $script:exitCode } }
if ($Unlock) { Unlock-TeamFiles $Unlock $Owner; if ($script:exitCode -ne 0 -or -not ($Build -or $Overview)) { exit $script:exitCode } }
if ($Migrate) { Invoke-Migration; $Build = $true }
if ($Restore) { Invoke-Restore $Restore; $Build = $true }
if ($UpdateFromSkill) { Invoke-SkillUpdate $UpdateFromSkill; if ($script:exitCode) { exit $script:exitCode }; $Build = $true }
$who = $Owner; if (-not $who) { $who = 'orchestrator' }
if ($SetStatus) { Invoke-SetStatus $SetStatus $who; $Build = $true }
if ($Reopen) { Invoke-Reopen $Reopen $Text $who; if ($script:exitCode) { exit $script:exitCode }; $Build = $true }
if ($Answer) { Invoke-Answer $Answer $Text $who; $Build = $true }
if ($Unblock) { Invoke-Unblock $who; $Build = $true }

$hasItems = $false
foreach ($k in $kinds.Keys) { if (Test-Path -LiteralPath (Join-Path $TeamFolder $kinds[$k].Folder)) { $hasItems = $true } }
$oldFile = Join-Path $TeamFolder 'decisions.md'
if (-not $hasItems -and (Test-Path -LiteralPath $oldFile) -and @(Read-Lines $oldFile | Where-Object { $_ -match '^## Task board' }).Count -gt 0) {
    Write-Output 'This team folder still has the old single decisions.md. Check and repair it, then run the script with -Migrate.'
    exit 1
}

$items = @(Get-Items $TeamFolder)
if ($Compact) { Invoke-Compact $items; $items = @(Get-Items $TeamFolder); $Build = $true }
$locks = @(Get-Locks)
Test-Items $items
Test-Gaps $items
Test-Waits $items
Test-Locks $locks $items
Test-Heartbeat $items
Test-Environment
Test-Decisions $items
Test-PullRequests $items
Test-TeamSetupCommits
Test-Scope $items
Test-Sprints $items
Test-Tickets $items
Test-DocumentSections
if ($script:stopState) {
    $st = $script:stopState
    $label = @{ finish = 'team stop (finish the running tasks, then review, approval and merge)'; now = 'EMERGENCY STOP (everyone stops at once)' }[$st.Mode]
    Add-Problem 'WARNING' 'stop.md' ('{0} since {1}, by {2}: {3}. Only the orchestrator ends it, with -Resume (team continue)' -f $label, $st.Since, $st.By, $st.Reason)
}

if (-not $Baseline) {
    $archiveDir = Join-Path $TeamFolder 'archive'
    if (Test-Path -LiteralPath $archiveDir) {
        $latest = Get-ChildItem -LiteralPath $archiveDir -Directory | Where-Object { $_.Name -like '*-ok' -and (Test-Path -LiteralPath (Join-Path $_.FullName 'project.md')) } |
            Sort-Object Name -Descending | Select-Object -First 1
        if ($latest) { $Baseline = $latest.FullName }
    }
}
Test-Documents $Baseline
if ($Baseline -and (Test-Path -LiteralPath $Baseline)) { Compare-WithBaseline $items $Baseline }

$team = Get-TeamStatus $items
if ($Baseline) { $against = " against $Baseline" } else { $against = ' (no -ok copy to compare with)' }
Write-Output ("Checked {0}: {1} tasks, {2} questions, {3} decisions, {4} requirements, {5} locks{6}" -f $TeamFolder,
    @($items | Where-Object { $_.Kind -eq 'task' }).Count, @($items | Where-Object { $_.Kind -eq 'question' }).Count,
    @($items | Where-Object { $_.Kind -eq 'decision' }).Count, @($items | Where-Object { $_.Kind -eq 'requirement' }).Count, $locks.Count, $against)
foreach ($e in $script:errors) { Write-Output $e }
foreach ($w in $script:warnings) { Write-Output $w }
Write-Output ("Result: {0} errors, {1} warnings" -f $script:errors.Count, $script:warnings.Count)

if ($Overview) { Write-Output ''; foreach ($l in (Get-StatusLines $team $locks)) { Write-Output $l } }
if ($Build) {
    Register-ExistingIds $items
    Build-Overview $items $team $locks
    Write-Output "Built $TeamFolder/decisions.md"
}
if ($Build -or $Report) {
    $reportLines = Build-Report $items $team
    if ($Report) { Write-Output ''; foreach ($l in $reportLines) { Write-Output $l } } else { Write-Output "Built $TeamFolder/report.md" }
}
if ($Build -or $Standup) {
    $standupLines = Build-Standup $items $team
    if ($Standup) { Write-Output ''; foreach ($l in $standupLines) { Write-Output $l } } else { Write-Output "Built $TeamFolder/standup.md" }
}

if ($script:errors.Count -gt 0) {
    Write-Output 'Fix the errors before anyone continues: restore broken item files from the latest -ok archive copy, and break every deadlock.'
    exit 1
}

if ($SaveOk) {
    Write-Output ('Saved a checked copy to ' + (Save-OkCopy '-ok'))
} elseif ($Build -and -not $script:somethingLost) {
    # Automatic checked copy: at most every 10 minutes, only without errors and without lost items,
    # so there is always a recent copy to restore a damaged file from. The newest 48 are kept (8 hours).
    $archiveDir = Join-Path $TeamFolder 'archive'
    $newest = $null
    if (Test-Path -LiteralPath $archiveDir) {
        foreach ($d in @(Get-ChildItem -LiteralPath $archiveDir -Directory | Where-Object { $_.Name -like '*-ok' })) {
            try { $t = [datetime]::ParseExact($d.Name.Substring(0, 15), 'yyyy-MM-dd-HHmm', [Globalization.CultureInfo]::InvariantCulture) } catch { continue }
            if (-not $newest -or $t -gt $newest) { $newest = $t }
        }
    }
    if (-not $newest -or ((Get-Date) - $newest).TotalMinutes -ge 10) {
        Write-Output ('Saved an automatic checked copy to ' + (Save-OkCopy '-auto-ok'))
        $autos = @(Get-ChildItem -LiteralPath $archiveDir -Directory | Where-Object { $_.Name -like '*-auto-ok' } | Sort-Object Name -Descending)
        foreach ($old in @($autos | Select-Object -Skip 48)) { Remove-Item -LiteralPath $old.FullName -Recurse -Force }
    }
}
exit 0
