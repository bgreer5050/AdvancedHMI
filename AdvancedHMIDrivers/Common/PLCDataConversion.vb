Public Class PLCDataConversion
    '****************************************************************
    '* Convert an array of words into a string as AB PLC's represent
    '* Can be used when reading a string from an Integer file
    '****************************************************************
    ''' <summary>
    ''' Convert an array of integers to a string
    ''' This is used when storing strings in an integer data table
    ''' </summary>
    ''' <param name="words"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function WordsToString(ByVal words() As Int32) As String
        Dim WordCount As Integer = words.Length
        Return WordsToString(words, 0, WordCount)
    End Function

    ''' <summary>
    ''' Convert an array of integers to a string
    ''' This is used when storing strings in an integer data table
    ''' </summary>
    ''' <param name="words"></param>
    ''' <param name="index"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function WordsToString(ByVal words() As Int32, ByVal index As Integer) As String
        Dim WordCount As Integer = (words.Length - index)
        Return WordsToString(words, index, WordCount)
    End Function

    ''' <summary>
    ''' Convert an array of integers to a string
    ''' This is used when storing strings in an integer data table
    ''' </summary>
    ''' <param name="words"></param>
    ''' <param name="index"></param>
    ''' <param name="wordCount"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function WordsToString(ByVal words() As Int32, ByVal index As Integer, ByVal wordCount As Integer) As String
        Dim j As Integer = index
        Dim result2 As New System.Text.StringBuilder
        While j < wordCount
            result2.Append(Chr(words(j) >> 8))
            '* Prevent an odd length string from getting a Null added on
            If CInt(words(j) And &HFF) > 0 Then
                result2.Append(Chr(words(j) And &HFF))
            End If
            j += 1
        End While

        Return result2.ToString
    End Function


    '**********************************************************
    '* Convert a string to an array of words
    '*  Can be used when writing a string to an Integer file
    '**********************************************************
    ''' <summary>
    ''' Convert a string to an array of words
    ''' Can be used when writing a string into an integer data table
    ''' </summary>
    ''' <param name="source"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Shared Function StringToWords(ByVal source As String) As Int32()
        If source Is Nothing Then
            Return Nothing
            ' Throw New ArgumentNullException("input")
        End If

        Dim ArraySize As Integer = CInt(Math.Ceiling(source.Length / 2)) - 1

        Dim ConvertedData(ArraySize) As Int32

        Dim i As Integer
        While i <= ArraySize
            ConvertedData(i) = Asc(source.Substring(i * 2, 1)) * 256
            '* Check if past last character of odd length string
            If (i * 2) + 1 < source.Length Then ConvertedData(i) += Asc(source.Substring((i * 2) + 1, 1))
            i += 1
        End While

        Return ConvertedData
    End Function
End Class
