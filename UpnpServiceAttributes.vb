Partial Public Class Plugin
    <AttributeUsage(AttributeTargets.[Class], AllowMultiple:=True)> _
    Private NotInheritable Class UpnpServiceVariable
        Inherits Attribute
        Private ReadOnly m_name As String
        Private ReadOnly m_dataType As String
        Private ReadOnly m_sendEvents As Boolean
        Private ReadOnly m_allowedValue As String()

        Public Sub New(name As String, dataType As String, sendEvents As Boolean, ParamArray allowedValue As String())
            m_name = name
            m_dataType = dataType
            m_sendEvents = sendEvents
            m_allowedValue = allowedValue
        End Sub

        Public Sub New(name As String, dataType As String, sendEvents As Boolean)
            Me.New(name, dataType, sendEvents, New String(-1) {})
        End Sub

        Public ReadOnly Property Name() As String
            Get
                Return m_name
            End Get
        End Property

        Public ReadOnly Property DataType() As String
            Get
                Return m_dataType
            End Get
        End Property

        Public ReadOnly Property SendEvents() As Boolean
            Get
                Return m_sendEvents
            End Get
        End Property

        Public ReadOnly Property AllowedValue() As String()
            Get
                Return m_allowedValue
            End Get
        End Property
    End Class  ' UpnpServiceVariable

    <AttributeUsage(AttributeTargets.Parameter Or AttributeTargets.Method, AllowMultiple:=True)> _
    Private NotInheritable Class UpnpServiceArgument
        Inherits Attribute
        Private ReadOnly m_index As Integer
        Private ReadOnly m_name As String
        Private ReadOnly m_relatedStateVariable As String

        Public Sub New(index As Integer, name As String, relatedStateVariable As String)
            m_index = index
            m_name = name
            m_relatedStateVariable = relatedStateVariable
        End Sub

        Public Sub New(relatedStateVariable As String)
            m_relatedStateVariable = relatedStateVariable
        End Sub

        Public ReadOnly Property Index() As Integer
            Get
                Return m_index
            End Get
        End Property

        Public ReadOnly Property Name() As String
            Get
                Return m_name
            End Get
        End Property

        Public ReadOnly Property RelatedStateVariable() As String
            Get
                Return m_relatedStateVariable
            End Get
        End Property
    End Class  ' UpnpServiceArgument
End Class