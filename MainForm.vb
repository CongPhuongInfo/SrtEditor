Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports System.Xml

''' <summary>
''' Công cụ chỉnh sửa tệp .srt (phụ đề) và .ssml (Speech Synthesis Markup Language),
''' kèm chuyển đổi qua lại giữa hai định dạng.
''' Gồm 3 tab: SRT Editor, SSML Editor, và Chuyen doi.
''' </summary>
Public Class MainForm
    Inherits Form

#Region "Controls"
    Private menuStrip1 As MenuStrip
    Private tabControl1 As TabControl
    Private tabSrt As TabPage
    Private tabSsml As TabPage
    Private tabConvert As TabPage

    ' --- SRT tab ---
    Private dgvSrt As DataGridView
    Private txtFind As TextBox
    Private txtReplace As TextBox
    Private numShiftSeconds As NumericUpDown
    Private numShiftMillis As NumericUpDown
    Private rbShiftPlus As RadioButton
    Private rbShiftMinus As RadioButton
    Private lblSrtStatus As Label

    ' --- SSML tab ---
    Private txtSsml As TextBox
    Private txtVoiceName As TextBox
    Private txtNewSentence As TextBox
    Private txtBreakTime As TextBox
    Private cmbEmphasisLevel As ComboBox
    Private lblSsmlStatus As Label

    ' --- Convert tab ---
    Private txtVoiceNameConv As TextBox
    Private chkFixedBreak As CheckBox
    Private numFixedBreakMs As NumericUpDown
    Private numCharsPerSec As NumericUpDown
    Private numMinDurationSec As NumericUpDown
    Private lblConvInfo As Label
#End Region

#Region "State"
    Private srtEntries As New List(Of SrtEntry)
    Private currentSrtPath As String = ""
    Private currentSsmlPath As String = ""
    Private loadingGrid As Boolean = False
#End Region

    Public Sub New()
        InitializeComponent()
    End Sub

#Region "Layout"

    Private Sub InitializeComponent()
        Me.Text = "SRT && SSML Editor"
        Me.Width = 1024
        Me.Height = 700
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Font = New Font("Segoe UI", 9.0!)

        BuildMenu()

        tabControl1 = New TabControl()
        tabControl1.Dock = DockStyle.Fill
        tabSrt = New TabPage("SRT Editor")
        tabSsml = New TabPage("SSML Editor")
        tabConvert = New TabPage("Chuyen doi SRT <-> SSML")
        tabControl1.TabPages.Add(tabSrt)
        tabControl1.TabPages.Add(tabSsml)
        tabControl1.TabPages.Add(tabConvert)
        Me.Controls.Add(tabControl1)
        tabControl1.BringToFront()

        BuildSrtTab()
        BuildSsmlTab()
        BuildConvertTab()
    End Sub

    Private Sub BuildMenu()
        menuStrip1 = New MenuStrip()

        Dim mFile As New ToolStripMenuItem("File")
        Dim mOpenSrt As New ToolStripMenuItem("Mo tep SRT...", Nothing, AddressOf OpenSrt_Click)
        Dim mSaveSrt As New ToolStripMenuItem("Luu SRT", Nothing, AddressOf SaveSrt_Click)
        Dim mSaveSrtAs As New ToolStripMenuItem("Luu SRT thanh...", Nothing, AddressOf SaveSrtAs_Click)
        Dim mOpenSsml As New ToolStripMenuItem("Mo tep SSML...", Nothing, AddressOf OpenSsml_Click)
        Dim mSaveSsml As New ToolStripMenuItem("Luu SSML", Nothing, AddressOf SaveSsml_Click)
        Dim mSaveSsmlAs As New ToolStripMenuItem("Luu SSML thanh...", Nothing, AddressOf SaveSsmlAs_Click)
        Dim mExit As New ToolStripMenuItem("Thoat", Nothing, AddressOf Exit_Click)

        mFile.DropDownItems.AddRange(New ToolStripItem() {
            mOpenSrt, mSaveSrt, mSaveSrtAs, New ToolStripSeparator(),
            mOpenSsml, mSaveSsml, mSaveSsmlAs, New ToolStripSeparator(),
            mExit})

        menuStrip1.Items.Add(mFile)
        Me.MainMenuStrip = menuStrip1
        Me.Controls.Add(menuStrip1)
    End Sub

    Private Sub BuildSrtTab()
        Dim panelTop As New Panel()
        panelTop.Dock = DockStyle.Top
        panelTop.Height = 122

        Dim lblFind As New Label() With {.Text = "Tim:", .Left = 10, .Top = 12, .Width = 40}
        txtFind = New TextBox() With {.Left = 55, .Top = 9, .Width = 150}
        Dim lblReplace As New Label() With {.Text = "Thay bang:", .Left = 215, .Top = 12, .Width = 70}
        txtReplace = New TextBox() With {.Left = 290, .Top = 9, .Width = 150}
        Dim btnFindReplace As New Button() With {.Text = "Tim && thay tat ca", .Left = 450, .Top = 7, .Width = 150}
        AddHandler btnFindReplace.Click, AddressOf BtnFindReplace_Click

        Dim lblShift As New Label() With {.Text = "Dich thoi gian:", .Left = 10, .Top = 47, .Width = 90}
        rbShiftPlus = New RadioButton() With {.Text = "+", .Left = 100, .Top = 46, .Width = 40, .Checked = True}
        rbShiftMinus = New RadioButton() With {.Text = "-", .Left = 140, .Top = 46, .Width = 40}
        numShiftSeconds = New NumericUpDown() With {.Left = 185, .Top = 44, .Width = 65, .Maximum = 359999, .Minimum = 0}
        Dim lblSec As New Label() With {.Text = "giay", .Left = 253, .Top = 47, .Width = 35}
        numShiftMillis = New NumericUpDown() With {.Left = 290, .Top = 44, .Width = 65, .Maximum = 999, .Minimum = 0}
        Dim lblMs As New Label() With {.Text = "ms", .Left = 358, .Top = 47, .Width = 30}
        Dim btnShiftTime As New Button() With {.Text = "Ap dung dich thoi gian", .Left = 450, .Top = 42, .Width = 150}
        AddHandler btnShiftTime.Click, AddressOf BtnShiftTime_Click

        Dim btnRenumber As New Button() With {.Text = "Danh so lai (1, 2, 3...)", .Left = 610, .Top = 7, .Width = 170}
        AddHandler btnRenumber.Click, AddressOf BtnRenumber_Click

        Dim btnLoadSrt As New Button() With {.Text = "Mo tep SRT...", .Left = 610, .Top = 42, .Width = 170}
        AddHandler btnLoadSrt.Click, AddressOf OpenSrt_Click

        Dim btnCheckOverlap As New Button() With {.Text = "Kiem tra overlap", .Left = 10, .Top = 82, .Width = 170}
        AddHandler btnCheckOverlap.Click, AddressOf BtnCheckOverlap_Click

        Dim btnFixOverlap As New Button() With {.Text = "Tu dong sua overlap", .Left = 190, .Top = 82, .Width = 190}
        AddHandler btnFixOverlap.Click, AddressOf BtnFixOverlap_Click

        panelTop.Controls.AddRange(New Control() {
            lblFind, txtFind, lblReplace, txtReplace, btnFindReplace,
            lblShift, rbShiftPlus, rbShiftMinus, numShiftSeconds, lblSec, numShiftMillis, lblMs, btnShiftTime,
            btnRenumber, btnLoadSrt, btnCheckOverlap, btnFixOverlap})

        dgvSrt = New DataGridView()
        dgvSrt.Dock = DockStyle.Fill
        dgvSrt.AllowUserToAddRows = False
        dgvSrt.AllowUserToDeleteRows = True
        dgvSrt.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvSrt.EditMode = DataGridViewEditMode.EditOnEnter
        dgvSrt.Columns.Add("colIndex", "Index")
        dgvSrt.Columns.Add("colStart", "Bat dau")
        dgvSrt.Columns.Add("colEnd", "Ket thuc")
        dgvSrt.Columns.Add("colText", "Noi dung (dung ' | ' de xuong dong)")
        dgvSrt.Columns("colIndex").Width = 60
        dgvSrt.Columns("colStart").Width = 110
        dgvSrt.Columns("colEnd").Width = 110
        AddHandler dgvSrt.CellEndEdit, AddressOf DgvSrt_CellEndEdit

        lblSrtStatus = New Label() With {
            .Dock = DockStyle.Bottom, .Height = 26, .Text = "Chua mo tep SRT nao.",
            .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(5, 0, 0, 0),
            .BorderStyle = BorderStyle.Fixed3D}

        tabSrt.Controls.Add(dgvSrt)
        tabSrt.Controls.Add(panelTop)
        tabSrt.Controls.Add(lblSrtStatus)
    End Sub

    Private Sub BuildSsmlTab()
        Dim panelTop As New Panel()
        panelTop.Dock = DockStyle.Top
        panelTop.Height = 112

        Dim lblVoice As New Label() With {.Text = "Ten giong (voice name):", .Left = 10, .Top = 12, .Width = 150}
        txtVoiceName = New TextBox() With {.Left = 165, .Top = 9, .Width = 180, .Text = "en-US-AriaNeural"}
        Dim btnSetVoice As New Button() With {.Text = "Ap dung cho <voice>", .Left = 355, .Top = 7, .Width = 160}
        AddHandler btnSetVoice.Click, AddressOf BtnSetVoice_Click

        Dim btnLoadSsml As New Button() With {.Text = "Mo tep SSML...", .Left = 830, .Top = 7, .Width = 150}
        AddHandler btnLoadSsml.Click, AddressOf OpenSsml_Click

        Dim lblSentence As New Label() With {.Text = "Cau moi:", .Left = 10, .Top = 47, .Width = 150}
        txtNewSentence = New TextBox() With {.Left = 165, .Top = 44, .Width = 320}
        Dim btnAddSentence As New Button() With {.Text = "Them <p><s>...</s></p>", .Left = 495, .Top = 42, .Width = 160}
        AddHandler btnAddSentence.Click, AddressOf BtnAddSentence_Click

        Dim btnFormatXml As New Button() With {.Text = "Dinh dang lai XML", .Left = 830, .Top = 42, .Width = 150}
        AddHandler btnFormatXml.Click, AddressOf BtnFormatXml_Click

        Dim lblBreak As New Label() With {.Text = "Break time (vd 500ms):", .Left = 10, .Top = 82, .Width = 150}
        txtBreakTime = New TextBox() With {.Left = 165, .Top = 79, .Width = 100, .Text = "500ms"}
        Dim btnAddBreak As New Button() With {.Text = "Chen <break> tai con tro", .Left = 275, .Top = 77, .Width = 190}
        AddHandler btnAddBreak.Click, AddressOf BtnAddBreak_Click

        cmbEmphasisLevel = New ComboBox() With {.Left = 495, .Top = 79, .Width = 100, .DropDownStyle = ComboBoxStyle.DropDownList}
        cmbEmphasisLevel.Items.AddRange(New Object() {"reduced", "moderate", "strong"})
        cmbEmphasisLevel.SelectedIndex = 1
        Dim btnWrapEmphasis As New Button() With {.Text = "Boc <emphasis> quanh chu da chon", .Left = 605, .Top = 77, .Width = 240}
        AddHandler btnWrapEmphasis.Click, AddressOf BtnWrapEmphasis_Click

        panelTop.Controls.AddRange(New Control() {
            lblVoice, txtVoiceName, btnSetVoice, btnLoadSsml,
            lblSentence, txtNewSentence, btnAddSentence, btnFormatXml,
            lblBreak, txtBreakTime, btnAddBreak, cmbEmphasisLevel, btnWrapEmphasis})

        txtSsml = New TextBox()
        txtSsml.Dock = DockStyle.Fill
        txtSsml.Multiline = True
        txtSsml.ScrollBars = ScrollBars.Both
        txtSsml.WordWrap = False
        txtSsml.Font = New Font("Consolas", 10.0!)
        txtSsml.AcceptsTab = True

        lblSsmlStatus = New Label() With {
            .Dock = DockStyle.Bottom, .Height = 26, .Text = "Chua mo tep SSML nao.",
            .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(5, 0, 0, 0),
            .BorderStyle = BorderStyle.Fixed3D}

        tabSsml.Controls.Add(txtSsml)
        tabSsml.Controls.Add(panelTop)
        tabSsml.Controls.Add(lblSsmlStatus)
    End Sub

    Private Sub BuildConvertTab()
        Dim info As New Label() With {
            .Left = 10, .Top = 10, .Width = 960, .Height = 40,
            .Text = "Chuyen doi dua tren du lieu dang co trong 2 tab kia: 'SRT -> SSML' se dung noi dung dang mo o tab SRT Editor; " &
                    "'SSML -> SRT' se dung noi dung dang mo o tab SSML Editor. Sau khi chuyen doi, ket qua se duoc dien vao tab tuong ung va tu dong chuyen sang tab do."
        }

        ' ---- Nhom SRT -> SSML ----
        Dim grpToSsml As New GroupBox() With {.Text = "SRT -> SSML", .Left = 10, .Top = 60, .Width = 480, .Height = 160}

        Dim lblVoiceConv As New Label() With {.Text = "Ten giong (voice name):", .Left = 15, .Top = 30, .Width = 160}
        txtVoiceNameConv = New TextBox() With {.Left = 180, .Top = 27, .Width = 200, .Text = "en-US-JennyNeural"}

        chkFixedBreak = New CheckBox() With {.Text = "Dung khoang lang co dinh (bo qua thoi gian that trong SRT):", .Left = 15, .Top = 65, .Width = 360}
        numFixedBreakMs = New NumericUpDown() With {.Left = 380, .Top = 63, .Width = 80, .Minimum = 0, .Maximum = 60000, .Value = 500}
        Dim lblMsConv As New Label() With {.Text = "ms", .Left = 465, .Top = 65, .Width = 30}

        Dim lblNote1 As New Label() With {
            .Left = 15, .Top = 95, .Width = 450, .Height = 30,
            .Text = "(Neu khong tick, khoang lang <break> se duoc tinh tu khoang cach thoi gian thuc te giua cac dong phu de trong SRT.)",
            .ForeColor = Color.DimGray}

        Dim btnSrtToSsml As New Button() With {.Text = "Chuyen doi SRT dang mo -> SSML", .Left = 15, .Top = 128, .Width = 260}
        AddHandler btnSrtToSsml.Click, AddressOf BtnSrtToSsml_Click

        grpToSsml.Controls.AddRange(New Control() {lblVoiceConv, txtVoiceNameConv, chkFixedBreak, numFixedBreakMs, lblMsConv, lblNote1, btnSrtToSsml})

        ' ---- Nhom SSML -> SRT ----
        Dim grpToSrt As New GroupBox() With {.Text = "SSML -> SRT", .Left = 510, .Top = 60, .Width = 480, .Height = 160}

        Dim lblCps As New Label() With {.Text = "Toc do doc uoc tinh (ky tu/giay):", .Left = 15, .Top = 30, .Width = 200}
        numCharsPerSec = New NumericUpDown() With {.Left = 220, .Top = 27, .Width = 70, .Minimum = 1, .Maximum = 100, .Value = 15}

        Dim lblMinDur As New Label() With {.Text = "Thoi luong toi thieu moi cau (giay):", .Left = 15, .Top = 65, .Width = 200}
        numMinDurationSec = New NumericUpDown() With {.Left = 220, .Top = 62, .Width = 70, .Minimum = 0, .Maximum = 60, .DecimalPlaces = 1, .Increment = 0.5D, .Value = 1}

        Dim lblNote2 As New Label() With {
            .Left = 15, .Top = 95, .Width = 450, .Height = 30,
            .Text = "(SSML khong luu thoi gian tuyet doi, nen thoi luong moi cau se duoc uoc tinh tu do dai van ban va cac tag <break>.)",
            .ForeColor = Color.DimGray}

        Dim btnSsmlToSrt As New Button() With {.Text = "Chuyen doi SSML dang mo -> SRT", .Left = 15, .Top = 128, .Width = 260}
        AddHandler btnSsmlToSrt.Click, AddressOf BtnSsmlToSrt_Click

        grpToSrt.Controls.AddRange(New Control() {lblCps, numCharsPerSec, lblMinDur, numMinDurationSec, lblNote2, btnSsmlToSrt})

        lblConvInfo = New Label() With {
            .Dock = DockStyle.Bottom, .Height = 26, .Text = "San sang chuyen doi.",
            .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(5, 0, 0, 0),
            .BorderStyle = BorderStyle.Fixed3D}

        tabConvert.Controls.Add(info)
        tabConvert.Controls.Add(grpToSsml)
        tabConvert.Controls.Add(grpToSrt)
        tabConvert.Controls.Add(lblConvInfo)
    End Sub

#End Region

#Region "SRT logic"

    Private Sub BtnCheckOverlap_Click(sender As Object, e As EventArgs)
        If srtEntries.Count = 0 Then
            MessageBox.Show("Hay mo mot tep SRT truoc.", "Thong bao")
            Return
        End If

        Dim issues As List(Of String) = CheckOverlaps(srtEntries)
        If issues.Count = 0 Then
            lblSrtStatus.Text = "Khong co loi overlap."
            MessageBox.Show("Khong phat hien loi overlap nao.", "Ket qua kiem tra")
        Else
            lblSrtStatus.Text = String.Format("Phat hien {0} loi overlap.", issues.Count)
            MessageBox.Show(String.Join(Environment.NewLine, issues),
                             String.Format("Phat hien {0} loi overlap", issues.Count),
                             MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    Private Sub BtnFixOverlap_Click(sender As Object, e As EventArgs)
        If srtEntries.Count = 0 Then
            MessageBox.Show("Hay mo mot tep SRT truoc.", "Thong bao")
            Return
        End If

        Dim fixedCount As Integer = FixOverlaps(srtEntries)
        FillSrtGrid()

        If fixedCount > 0 Then
            lblSrtStatus.Text = String.Format("Da tu dong sua {0} loi overlap. Nho bam 'Luu SRT' de ghi lai tep.", fixedCount)
            MessageBox.Show(String.Format("Da sua {0} loi overlap. Kiem tra lai bang roi bam 'Luu SRT' de ghi tep.", fixedCount), "Da sua overlap")
        Else
            lblSrtStatus.Text = "Khong co loi overlap can sua."
            MessageBox.Show("Khong phat hien loi overlap nao.", "Thong bao")
        End If
    End Sub

    ''' <summary>
    ''' Duyet toan bo danh sach entries theo dung thu tu hien co trong bang va tra ve
    ''' MOI loi overlap tim thay (dong sau co StartTime nho hon EndTime cua dong truoc).
    ''' </summary>
    Private Function CheckOverlaps(entries As List(Of SrtEntry)) As List(Of String)
        Dim issues As New List(Of String)
        Dim lastEndTime As TimeSpan = TimeSpan.Zero

        For i As Integer = 0 To entries.Count - 1
            Dim entry As SrtEntry = entries(i)
            If i > 0 AndAlso entry.StartTime < lastEndTime Then
                issues.Add(String.Format("Dong {0} (index {1}): bat dau {2} nho hon ket thuc cua dong truoc ({3})",
                    i + 1, entry.Index, SrtEntry.FormatTime(entry.StartTime), SrtEntry.FormatTime(lastEndTime)))
            End If
            If entry.EndTime > lastEndTime Then lastEndTime = entry.EndTime
        Next

        Return issues
    End Function

    ''' <summary>
    ''' Tu dong sua loi overlap ngay tren du lieu dang co (chua ghi tep): neu StartTime cua
    ''' dong hien tai nho hon EndTime cua dong truoc, day StartTime len sau EndTime do 1ms;
    ''' neu viec do lam EndTime &lt;= StartTime (overlap qua lon) thi day ca EndTime len
    ''' toi thieu 500ms de tranh sinh thoi luong am/bang 0.
    ''' </summary>
    Private Function FixOverlaps(entries As List(Of SrtEntry)) As Integer
        Dim lastEndTime As TimeSpan = TimeSpan.Zero
        Dim fixedCount As Integer = 0
        Dim minDuration As TimeSpan = TimeSpan.FromMilliseconds(500)

        For i As Integer = 0 To entries.Count - 1
            Dim entry As SrtEntry = entries(i)
            Dim wasFixed As Boolean = False

            If i > 0 AndAlso entry.StartTime < lastEndTime Then
                entry.StartTime = lastEndTime.Add(TimeSpan.FromMilliseconds(1))
                wasFixed = True
            End If

            If entry.EndTime <= entry.StartTime Then
                entry.EndTime = entry.StartTime.Add(minDuration)
                wasFixed = True
            End If

            If wasFixed Then fixedCount += 1
            lastEndTime = entry.EndTime
        Next

        Return fixedCount
    End Function

    Private Sub OpenSrt_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Filter = "Tep SRT (*.srt)|*.srt|Tat ca tep (*.*)|*.*"
            If ofd.ShowDialog() = DialogResult.OK Then
                Try
                    Dim content As String = File.ReadAllText(ofd.FileName, Encoding.UTF8)
                    srtEntries = ParseSrt(content)
                    currentSrtPath = ofd.FileName
                    FillSrtGrid()
                    lblSrtStatus.Text = String.Format("Da mo: {0}  ({1} dong phu de)", ofd.FileName, srtEntries.Count)
                Catch ex As Exception
                    MessageBox.Show("Loi khi doc tep SRT: " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    Private Function ParseSrt(content As String) As List(Of SrtEntry)
        Dim entries As New List(Of SrtEntry)

        ' Chuan hoa ky tu xuong dong
        content = content.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf)
        Dim blocks As String() = Regex.Split(content.Trim(), "\n\s*\n")

        For Each block As String In blocks
            If String.IsNullOrWhiteSpace(block) Then Continue For
            Dim lines As String() = block.Split(New Char() {Chr(10)}, StringSplitOptions.None)
            If lines.Length < 2 Then Continue For

            Dim idx As Integer
            If Not Integer.TryParse(lines(0).Trim(), idx) Then Continue For

            Dim timeMatch As Match = Regex.Match(lines(1), "(\d{2}:\d{2}:\d{2},\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2},\d{3})")
            If Not timeMatch.Success Then Continue For

            Dim startTs As TimeSpan = ParseSrtTime(timeMatch.Groups(1).Value)
            Dim endTs As TimeSpan = ParseSrtTime(timeMatch.Groups(2).Value)

            Dim textLines As New List(Of String)
            For i As Integer = 2 To lines.Length - 1
                textLines.Add(lines(i))
            Next

            entries.Add(New SrtEntry() With {
                .Index = idx,
                .StartTime = startTs,
                .EndTime = endTs,
                .Text = String.Join(Environment.NewLine, textLines).Trim()
            })
        Next

        Return entries
    End Function

    Private Function ParseSrtTime(s As String) As TimeSpan
        Dim parts As String() = s.Trim().Split(","c)
        Dim hms As String() = parts(0).Split(":"c)
        Return New TimeSpan(0, Integer.Parse(hms(0)), Integer.Parse(hms(1)), Integer.Parse(hms(2)), Integer.Parse(parts(1)))
    End Function

    Private Sub FillSrtGrid()
        loadingGrid = True
        dgvSrt.Rows.Clear()
        For Each entry As SrtEntry In srtEntries
            dgvSrt.Rows.Add(entry.Index, SrtEntry.FormatTime(entry.StartTime), SrtEntry.FormatTime(entry.EndTime),
                             entry.Text.Replace(Environment.NewLine, " | "))
        Next
        loadingGrid = False
    End Sub

    Private Sub DgvSrt_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If loadingGrid Then Return
        If e.RowIndex < 0 OrElse e.RowIndex >= srtEntries.Count Then Return

        Dim row As DataGridViewRow = dgvSrt.Rows(e.RowIndex)
        Dim entry As SrtEntry = srtEntries(e.RowIndex)

        Select Case e.ColumnIndex
            Case 0
                Dim newIdx As Integer
                If Integer.TryParse(Convert.ToString(row.Cells(0).Value), newIdx) Then
                    entry.Index = newIdx
                End If
            Case 1
                Try
                    entry.StartTime = ParseSrtTime(Convert.ToString(row.Cells(1).Value))
                Catch
                    MessageBox.Show("Dinh dang thoi gian khong hop le. Vi du: 00:00:01,000", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    row.Cells(1).Value = SrtEntry.FormatTime(entry.StartTime)
                End Try
            Case 2
                Try
                    entry.EndTime = ParseSrtTime(Convert.ToString(row.Cells(2).Value))
                Catch
                    MessageBox.Show("Dinh dang thoi gian khong hop le. Vi du: 00:00:04,000", "Loi", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    row.Cells(2).Value = SrtEntry.FormatTime(entry.EndTime)
                End Try
            Case 3
                entry.Text = Convert.ToString(row.Cells(3).Value).Replace(" | ", Environment.NewLine)
        End Select
    End Sub

    Private Sub BtnFindReplace_Click(sender As Object, e As EventArgs)
        If srtEntries.Count = 0 Then
            MessageBox.Show("Hay mo mot tep SRT truoc.", "Thong bao")
            Return
        End If
        If String.IsNullOrEmpty(txtFind.Text) Then Return

        Dim count As Integer = 0
        For Each entry As SrtEntry In srtEntries
            If entry.Text.Contains(txtFind.Text) Then
                entry.Text = entry.Text.Replace(txtFind.Text, txtReplace.Text)
                count += 1
            End If
        Next
        FillSrtGrid()
        lblSrtStatus.Text = String.Format("Da thay the trong {0} dong phu de.", count)
    End Sub

    Private Sub BtnShiftTime_Click(sender As Object, e As EventArgs)
        If srtEntries.Count = 0 Then
            MessageBox.Show("Hay mo mot tep SRT truoc.", "Thong bao")
            Return
        End If

        Dim offset As New TimeSpan(0, 0, 0, CInt(numShiftSeconds.Value), CInt(numShiftMillis.Value))
        If rbShiftMinus.Checked Then offset = offset.Negate()

        For Each entry As SrtEntry In srtEntries
            Dim newStart As TimeSpan = entry.StartTime.Add(offset)
            Dim newEnd As TimeSpan = entry.EndTime.Add(offset)
            If newStart < TimeSpan.Zero Then newStart = TimeSpan.Zero
            If newEnd < TimeSpan.Zero Then newEnd = TimeSpan.Zero
            entry.StartTime = newStart
            entry.EndTime = newEnd
        Next
        FillSrtGrid()
        lblSrtStatus.Text = "Da dich thoi gian cho tat ca dong phu de."
    End Sub

    Private Sub BtnRenumber_Click(sender As Object, e As EventArgs)
        For i As Integer = 0 To srtEntries.Count - 1
            srtEntries(i).Index = i + 1
        Next
        FillSrtGrid()
        lblSrtStatus.Text = "Da danh so lai tu 1."
    End Sub

    Private Sub SaveSrt_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(currentSrtPath) Then
            SaveSrtAs_Click(sender, e)
            Return
        End If
        WriteSrt(currentSrtPath)
    End Sub

    Private Sub SaveSrtAs_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog()
            sfd.Filter = "Tep SRT (*.srt)|*.srt|Tat ca tep (*.*)|*.*"
            sfd.FileName = "output.srt"
            If sfd.ShowDialog() = DialogResult.OK Then
                currentSrtPath = sfd.FileName
                WriteSrt(sfd.FileName)
            End If
        End Using
    End Sub

    Private Sub WriteSrt(path As String)
        Try
            Dim sb As New StringBuilder()
            For Each entry As SrtEntry In srtEntries
                sb.Append(entry.ToBlock())
                sb.Append(Environment.NewLine)
            Next
            File.WriteAllText(path, sb.ToString().TrimEnd() & Environment.NewLine, New UTF8Encoding(False))
            lblSrtStatus.Text = "Da luu: " & path
        Catch ex As Exception
            MessageBox.Show("Loi khi luu tep SRT: " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

#End Region

#Region "SSML logic"

    Private Sub OpenSsml_Click(sender As Object, e As EventArgs)
        Using ofd As New OpenFileDialog()
            ofd.Filter = "Tep SSML (*.ssml;*.xml)|*.ssml;*.xml|Tat ca tep (*.*)|*.*"
            If ofd.ShowDialog() = DialogResult.OK Then
                Try
                    txtSsml.Text = File.ReadAllText(ofd.FileName, Encoding.UTF8)
                    currentSsmlPath = ofd.FileName
                    lblSsmlStatus.Text = "Da mo: " & ofd.FileName
                Catch ex As Exception
                    MessageBox.Show("Loi khi doc tep SSML: " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    Private Function LoadSsmlDoc() As XmlDocument
        Dim doc As New XmlDocument()
        doc.LoadXml(txtSsml.Text)
        Return doc
    End Function

    Private Sub BtnSetVoice_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtSsml.Text) Then
            MessageBox.Show("Hay mo mot tep SSML truoc.", "Thong bao")
            Return
        End If
        Try
            Dim doc As XmlDocument = LoadSsmlDoc()
            Dim voiceNodes As XmlNodeList = doc.GetElementsByTagName("voice")
            If voiceNodes.Count = 0 Then
                MessageBox.Show("Khong tim thay phan tu <voice> nao trong tep.", "Thong bao")
                Return
            End If
            For Each node As XmlNode In voiceNodes
                If node.Attributes IsNot Nothing AndAlso node.Attributes("name") IsNot Nothing Then
                    node.Attributes("name").Value = txtVoiceName.Text
                End If
            Next
            txtSsml.Text = FormatXml(doc)
            lblSsmlStatus.Text = String.Format("Da doi giong cho {0} phan tu <voice>.", voiceNodes.Count)
        Catch ex As Exception
            MessageBox.Show("Tep SSML khong hop le (loi XML): " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnAddSentence_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtSsml.Text) Then
            MessageBox.Show("Hay mo mot tep SSML truoc.", "Thong bao")
            Return
        End If
        If String.IsNullOrWhiteSpace(txtNewSentence.Text) Then Return
        Try
            Dim doc As XmlDocument = LoadSsmlDoc()
            Dim root As XmlElement = doc.DocumentElement
            If root Is Nothing Then Return

            Dim p As XmlElement = doc.CreateElement("p")
            Dim s As XmlElement = doc.CreateElement("s")
            s.InnerText = txtNewSentence.Text
            p.AppendChild(s)
            root.AppendChild(p)

            txtSsml.Text = FormatXml(doc)
            lblSsmlStatus.Text = "Da them <p><s> moi vao cuoi tai lieu."
        Catch ex As Exception
            MessageBox.Show("Tep SSML khong hop le (loi XML): " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnAddBreak_Click(sender As Object, e As EventArgs)
        Dim tag As String = String.Format("<break time=""{0}""/>", If(String.IsNullOrWhiteSpace(txtBreakTime.Text), "500ms", txtBreakTime.Text))
        Dim pos As Integer = txtSsml.SelectionStart
        txtSsml.Text = txtSsml.Text.Insert(pos, tag)
        txtSsml.SelectionStart = pos + tag.Length
        txtSsml.Focus()
        lblSsmlStatus.Text = "Da chen <break> tai vi tri con tro."
    End Sub

    Private Sub BtnWrapEmphasis_Click(sender As Object, e As EventArgs)
        If txtSsml.SelectionLength = 0 Then
            MessageBox.Show("Hay bo den (chon) doan chu can nhan manh truoc.", "Thong bao")
            Return
        End If
        Dim selected As String = txtSsml.SelectedText
        Dim level As String = If(cmbEmphasisLevel.SelectedItem IsNot Nothing, cmbEmphasisLevel.SelectedItem.ToString(), "moderate")
        Dim wrapped As String = String.Format("<emphasis level=""{0}"">{1}</emphasis>", level, selected)
        Dim startPos As Integer = txtSsml.SelectionStart
        txtSsml.SelectedText = wrapped
        txtSsml.SelectionStart = startPos
        txtSsml.SelectionLength = wrapped.Length
        lblSsmlStatus.Text = "Da boc <emphasis> quanh doan chu da chon."
    End Sub

    Private Sub BtnFormatXml_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtSsml.Text) Then Return
        Try
            Dim doc As XmlDocument = LoadSsmlDoc()
            txtSsml.Text = FormatXml(doc)
            lblSsmlStatus.Text = "Da dinh dang lai XML."
        Catch ex As Exception
            MessageBox.Show("Tep SSML khong hop le (loi XML): " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function FormatXml(doc As XmlDocument) As String
        Dim sw As New StringWriter()
        Dim xw As New XmlTextWriter(sw) With {.Formatting = Formatting.Indented, .Indentation = 2}
        doc.WriteTo(xw)
        Return sw.ToString()
    End Function

    Private Sub SaveSsml_Click(sender As Object, e As EventArgs)
        If String.IsNullOrEmpty(currentSsmlPath) Then
            SaveSsmlAs_Click(sender, e)
            Return
        End If
        WriteSsml(currentSsmlPath)
    End Sub

    Private Sub SaveSsmlAs_Click(sender As Object, e As EventArgs)
        Using sfd As New SaveFileDialog()
            sfd.Filter = "Tep SSML (*.ssml)|*.ssml|Tep XML (*.xml)|*.xml|Tat ca tep (*.*)|*.*"
            sfd.FileName = "edited.ssml"
            If sfd.ShowDialog() = DialogResult.OK Then
                currentSsmlPath = sfd.FileName
                WriteSsml(sfd.FileName)
            End If
        End Using
    End Sub

    Private Sub WriteSsml(path As String)
        Try
            File.WriteAllText(path, txtSsml.Text, New UTF8Encoding(False))
            lblSsmlStatus.Text = "Da luu: " & path
        Catch ex As Exception
            MessageBox.Show("Loi khi luu tep SSML: " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

#End Region

#Region "Convert logic (SRT <-> SSML)"

    Private Sub BtnSrtToSsml_Click(sender As Object, e As EventArgs)
        If srtEntries.Count = 0 Then
            MessageBox.Show("Hay mo mot tep SRT truoc (o tab SRT Editor).", "Thong bao")
            Return
        End If
        Try
            Dim ssml As String = BuildSsmlFromSrt(srtEntries, txtVoiceNameConv.Text, chkFixedBreak.Checked, CInt(numFixedBreakMs.Value))
            txtSsml.Text = ssml
            currentSsmlPath = ""
            tabControl1.SelectedTab = tabSsml
            lblSsmlStatus.Text = String.Format("Da chuyen doi tu SRT dang mo ({0} dong).", srtEntries.Count)
            lblConvInfo.Text = String.Format("Da tao SSML tu {0} dong phu de.", srtEntries.Count)
        Catch ex As Exception
            MessageBox.Show("Loi khi chuyen doi SRT -> SSML: " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnSsmlToSrt_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtSsml.Text) Then
            MessageBox.Show("Hay mo mot tep SSML truoc (o tab SSML Editor).", "Thong bao")
            Return
        End If
        Try
            Dim doc As XmlDocument = LoadSsmlDoc()
            Dim newEntries As List(Of SrtEntry) = BuildSrtFromSsml(doc, CDbl(numCharsPerSec.Value), CDbl(numMinDurationSec.Value))
            If newEntries.Count = 0 Then
                MessageBox.Show("Khong tim thay cau <s> nao trong tep SSML.", "Thong bao")
                Return
            End If
            srtEntries = newEntries
            currentSrtPath = ""
            FillSrtGrid()
            tabControl1.SelectedTab = tabSrt
            lblSrtStatus.Text = String.Format("Da chuyen doi tu SSML dang mo ({0} dong, thoi gian la uoc tinh).", srtEntries.Count)
            lblConvInfo.Text = String.Format("Da tao {0} dong SRT (thoi gian uoc tinh tu do dai van ban + <break>).", srtEntries.Count)
        Catch ex As Exception
            MessageBox.Show("Tep SSML khong hop le (loi XML): " & ex.Message, "Loi", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Xay dung noi dung SSML (dang <speak><voice><p><s>...) tu danh sach SrtEntry.
    ''' Khoang lang <break> giua cac cau duoc lay tu khoang cach thoi gian thuc te
    ''' trong SRT (End cua cau truoc -> Start cua cau sau), tru khi dung khoang lang co dinh.
    ''' </summary>
    Private Function BuildSsmlFromSrt(entries As List(Of SrtEntry), voiceName As String, useFixedBreak As Boolean, fixedMs As Integer) As String
        Dim sorted As List(Of SrtEntry) = entries.OrderBy(Function(x) x.StartTime).ToList()

        Dim doc As New XmlDocument()
        Dim speak As XmlElement = doc.CreateElement("speak")
        doc.AppendChild(speak)

        Dim voice As XmlElement = doc.CreateElement("voice")
        voice.SetAttribute("name", If(String.IsNullOrWhiteSpace(voiceName), "en-US-JennyNeural", voiceName))
        speak.AppendChild(voice)

        Dim prevEnd As TimeSpan? = Nothing
        For Each entry As SrtEntry In sorted
            If prevEnd.HasValue Then
                Dim gap As TimeSpan
                If useFixedBreak Then
                    gap = TimeSpan.FromMilliseconds(fixedMs)
                Else
                    gap = entry.StartTime - prevEnd.Value
                    If gap < TimeSpan.Zero Then gap = TimeSpan.Zero
                End If
                If gap > TimeSpan.Zero Then
                    Dim brk As XmlElement = doc.CreateElement("break")
                    brk.SetAttribute("time", CLng(gap.TotalMilliseconds).ToString(CultureInfo.InvariantCulture) & "ms")
                    voice.AppendChild(brk)
                End If
            End If

            Dim p As XmlElement = doc.CreateElement("p")
            Dim s As XmlElement = doc.CreateElement("s")
            s.InnerText = entry.Text.Replace(Environment.NewLine, " ").Trim()
            p.AppendChild(s)
            voice.AppendChild(p)

            prevEnd = entry.EndTime
        Next

        Return FormatXml(doc)
    End Function

    ''' <summary>
    ''' Xay dung danh sach SrtEntry tu mot tai lieu SSML da parse.
    ''' Vi SSML khong luu thoi gian tuyet doi cho tung cau, thoi gian bat dau/ket
    ''' thuc duoc uoc tinh: <break time="..."> cong don khoang lang, con moi <s>
    ''' duoc gan thoi luong = Max(minDurationSec, so_ky_tu / charsPerSecond).
    ''' </summary>
    Private Function BuildSrtFromSsml(doc As XmlDocument, charsPerSecond As Double, minDurationSec As Double) As List(Of SrtEntry)
        Dim result As New List(Of SrtEntry)
        If charsPerSecond <= 0 Then charsPerSecond = 15

        Dim nodes As XmlNodeList = doc.SelectNodes("//break | //s")
        Dim cumulative As TimeSpan = TimeSpan.Zero
        Dim idx As Integer = 1

        For Each node As XmlNode In nodes
            If node.Name = "break" Then
                Dim timeAttr As String = ""
                If node.Attributes IsNot Nothing AndAlso node.Attributes("time") IsNot Nothing Then
                    timeAttr = node.Attributes("time").Value
                End If
                cumulative = cumulative.Add(ParseBreakTime(timeAttr))

            ElseIf node.Name = "s" Then
                Dim text As String = node.InnerText.Trim()
                If text.Length = 0 Then Continue For

                Dim estSeconds As Double = Math.Max(minDurationSec, text.Length / charsPerSecond)
                Dim duration As TimeSpan = TimeSpan.FromSeconds(estSeconds)

                Dim entry As New SrtEntry()
                entry.Index = idx
                entry.StartTime = cumulative
                entry.EndTime = cumulative.Add(duration)
                entry.Text = text
                result.Add(entry)

                cumulative = entry.EndTime
                idx += 1
            End If
        Next

        Return result
    End Function

    ''' <summary>
    ''' Phan tich chuoi thoi gian kieu SSML ("500ms", "0.5s", "2s"...) thanh TimeSpan.
    ''' </summary>
    Private Function ParseBreakTime(s As String) As TimeSpan
        If String.IsNullOrWhiteSpace(s) Then Return TimeSpan.Zero
        s = s.Trim().ToLowerInvariant()
        Try
            If s.EndsWith("ms") Then
                Dim v As Double = Double.Parse(s.Substring(0, s.Length - 2), CultureInfo.InvariantCulture)
                Return TimeSpan.FromMilliseconds(v)
            ElseIf s.EndsWith("s") Then
                Dim v As Double = Double.Parse(s.Substring(0, s.Length - 1), CultureInfo.InvariantCulture)
                Return TimeSpan.FromSeconds(v)
            Else
                Dim v As Double = Double.Parse(s, CultureInfo.InvariantCulture)
                Return TimeSpan.FromSeconds(v)
            End If
        Catch
            Return TimeSpan.Zero
        End Try
    End Function

#End Region

    Private Sub Exit_Click(sender As Object, e As EventArgs)
        Me.Close()
    End Sub

End Class
