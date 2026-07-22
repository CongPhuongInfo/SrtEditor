Imports System

''' <summary>
''' Đại diện cho một mục (block) phụ đề trong tệp .srt:
''' số thứ tự, thời gian bắt đầu/kết thúc, và nội dung văn bản.
''' </summary>
Public Class SrtEntry
    Public Property Index As Integer
    Public Property StartTime As TimeSpan
    Public Property EndTime As TimeSpan
    Public Property Text As String = ""

    ''' <summary>
    ''' Định dạng một TimeSpan theo chuẩn SRT: HH:MM:SS,mmm
    ''' </summary>
    Public Shared Function FormatTime(t As TimeSpan) As String
        Return String.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}",
            CInt(Math.Floor(t.TotalHours)), t.Minutes, t.Seconds, t.Milliseconds)
    End Function

    ''' <summary>
    ''' Xuất mục này ra dạng khối văn bản chuẩn SRT (index, dòng thời gian, nội dung).
    ''' </summary>
    Public Function ToBlock() As String
        Return Index.ToString() & Environment.NewLine &
               FormatTime(StartTime) & " --> " & FormatTime(EndTime) & Environment.NewLine &
               Text.TrimEnd() & Environment.NewLine
    End Function
End Class
