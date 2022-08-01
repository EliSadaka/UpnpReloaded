Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms.VisualStyles

Partial Public Class Plugin
    Private NotInheritable Class SplitButton
        Inherits Button
        Private Const splitSectionWidth As Integer = 18
        Private pushState As PushButtonState
        Private skipNextOpen As Boolean
        Private dropDownRectangle As Rectangle
        Private isSplitMenuVisible As Boolean
        Private isMouseEntered As Boolean
        Private splitMenuStrip As ContextMenuStrip
        Private ReadOnly textFlags As TextFormatFlags = TextFormatFlags.Default
        Private Shared ReadOnly borderSize As Integer = SystemInformation.Border3DSize.Width * 2

        Public Sub New()
            AutoSize = True
        End Sub

        <Browsable(False)> _
        Public Overrides Property ContextMenuStrip() As ContextMenuStrip
            Get
                Return splitMenuStrip
            End Get
            Set(value As ContextMenuStrip)
                AddHandler value.Closing, AddressOf SplitMenuStrip_Closing
                AddHandler value.Opening, AddressOf SplitMenuStrip_Opening
                splitMenuStrip = value
            End Set
        End Property

        Private Property State() As PushButtonState
            Get
                Return pushState
            End Get
            Set(value As PushButtonState)
                If Not pushState.Equals(value) Then
                    pushState = value
                    Invalidate()
                End If
            End Set
        End Property

        Protected Overrides Function IsInputKey(keyData As Keys) As Boolean
            If keyData.Equals(Keys.Down) Then
                Return True
            End If
            Return MyBase.IsInputKey(keyData)
        End Function

        Protected Overrides Sub OnGotFocus(e As EventArgs)
            If Not State.Equals(PushButtonState.Pressed) AndAlso Not State.Equals(PushButtonState.Disabled) Then
                State = PushButtonState.[Default]
            End If
        End Sub

        Protected Overrides Sub OnKeyDown(kevent As KeyEventArgs)
            If kevent.KeyCode.Equals(Keys.Down) AndAlso Not isSplitMenuVisible Then
                ShowContextMenuStrip()
            ElseIf kevent.KeyCode.Equals(Keys.Space) AndAlso kevent.Modifiers = Keys.None Then
                State = PushButtonState.Pressed
            End If
            MyBase.OnKeyDown(kevent)
        End Sub

        Protected Overrides Sub OnKeyUp(kevent As KeyEventArgs)
            If kevent.KeyCode.Equals(Keys.Space) Then
                If MouseButtons = MouseButtons.None Then
                    State = PushButtonState.Normal
                End If
            ElseIf kevent.KeyCode.Equals(Keys.Apps) Then
                If MouseButtons = MouseButtons.None AndAlso Not isSplitMenuVisible Then
                    ShowContextMenuStrip()
                End If
            End If
            MyBase.OnKeyUp(kevent)
        End Sub

        Protected Overrides Sub OnEnabledChanged(e As EventArgs)
            State = If(Enabled, PushButtonState.Normal, PushButtonState.Disabled)
            MyBase.OnEnabledChanged(e)
        End Sub

        Protected Overrides Sub OnLostFocus(e As EventArgs)
            If Not State.Equals(PushButtonState.Pressed) AndAlso Not State.Equals(PushButtonState.Disabled) Then
                State = PushButtonState.Normal
            End If
        End Sub

        Protected Overrides Sub OnMouseEnter(e As EventArgs)
            isMouseEntered = True
            If Not State.Equals(PushButtonState.Pressed) AndAlso Not State.Equals(PushButtonState.Disabled) Then
                State = PushButtonState.Hot
            End If
        End Sub

        Protected Overrides Sub OnMouseLeave(e As EventArgs)
            isMouseEntered = False
            If Not State.Equals(PushButtonState.Pressed) AndAlso Not State.Equals(PushButtonState.Disabled) Then
                State = If(Focused, PushButtonState.[Default], PushButtonState.Normal)
            End If
        End Sub

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            If dropDownRectangle.Contains(e.Location) AndAlso Not isSplitMenuVisible AndAlso e.Button = MouseButtons.Left Then
                ShowContextMenuStrip()
            Else
                State = PushButtonState.Pressed
            End If
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            If e.Button = MouseButtons.Right AndAlso ClientRectangle.Contains(e.Location) AndAlso Not isSplitMenuVisible Then
                ShowContextMenuStrip()
            ElseIf splitMenuStrip Is Nothing OrElse Not isSplitMenuVisible Then
                SetButtonDrawState()
                If ClientRectangle.Contains(e.Location) AndAlso Not dropDownRectangle.Contains(e.Location) Then
                    OnClick(New EventArgs())
                End If
            End If
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim g As Graphics = e.Graphics
            Dim bounds As Rectangle = ClientRectangle
            If State <> PushButtonState.Pressed AndAlso IsDefault AndAlso Not Application.RenderWithVisualStyles Then
                Dim backgroundBounds As Rectangle = bounds
                backgroundBounds.Inflate(-1, -1)
                ButtonRenderer.DrawButton(g, backgroundBounds, State)
                g.DrawRectangle(SystemPens.WindowFrame, 0, 0, bounds.Width - 1, bounds.Height - 1)
            Else
                ButtonRenderer.DrawButton(g, bounds, State)
            End If
            dropDownRectangle = New Rectangle(bounds.Right - splitSectionWidth, 0, splitSectionWidth, bounds.Height)
            Dim internalBorder As Integer = borderSize
            Dim focusRect As New Rectangle(internalBorder - 1, internalBorder - 1, bounds.Width - dropDownRectangle.Width - internalBorder, bounds.Height - (internalBorder * 2) + 2)
            Dim drawSplitLine As Boolean = (State = PushButtonState.Hot OrElse State = PushButtonState.Pressed OrElse Not Application.RenderWithVisualStyles)
            If drawSplitLine Then
                g.DrawLine(SystemPens.ButtonShadow, bounds.Right - splitSectionWidth, borderSize, bounds.Right - splitSectionWidth, bounds.Bottom - borderSize)
                g.DrawLine(SystemPens.ButtonFace, bounds.Right - splitSectionWidth - 1, borderSize, bounds.Right - splitSectionWidth - 1, bounds.Bottom - borderSize)
            End If
            PaintArrow(g, dropDownRectangle)
            PaintTextandImage(g, New Rectangle(0, 0, ClientRectangle.Width - splitSectionWidth, ClientRectangle.Height))
            If State <> PushButtonState.Pressed AndAlso Focused AndAlso ShowFocusCues Then
                ControlPaint.DrawFocusRectangle(g, focusRect)
            End If
        End Sub

        Private Sub PaintTextandImage(g As Graphics, bounds As Rectangle)
            Dim textSize As Size = TextRenderer.MeasureText(Text, Font, bounds.Size, textFlags)
            Dim textRect As New Rectangle((bounds.Width - textSize.Width) \ 2, (bounds.Height - textSize.Height) \ 2, textSize.Width, textSize.Height)
            If pushState = PushButtonState.Pressed AndAlso Not Application.RenderWithVisualStyles Then
                textRect.Offset(1, 1)
            End If
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor, (textFlags Or TextFormatFlags.NoPrefix))
        End Sub

        Private Sub PaintArrow(g As Graphics, dropDownRect As Rectangle)
            Dim middle As New Point(Convert.ToInt32(dropDownRect.Left + dropDownRect.Width / 2), Convert.ToInt32(dropDownRect.Top + dropDownRect.Height / 2))
            middle.X -= ((dropDownRect.Width Mod 2) + 1)
            Dim arrow() As Point = New Point() {New Point(middle.X - 2, middle.Y - 1), New Point(middle.X + 3, middle.Y - 1), New Point(middle.X, middle.Y + 2)}
            g.FillPolygon(SystemBrushes.ControlText, arrow)
        End Sub

        Public Overrides Function GetPreferredSize(proposedSize As Size) As Size
            Dim preferredSize As Size = MyBase.GetPreferredSize(proposedSize)
            If AutoSize Then
                Return CalculateButtonAutoSize()
            End If
            If Not String.IsNullOrEmpty(Text) AndAlso TextRenderer.MeasureText(Text, Font).Width + splitSectionWidth > preferredSize.Width Then
                Return preferredSize + New Size(splitSectionWidth + borderSize * 2, 0)
            End If
            Return preferredSize
        End Function

        Private Function CalculateButtonAutoSize() As Size
            Dim textSize As Size = TextRenderer.MeasureText(Text, Font)
            Dim result As Size
            result.Height = textSize.Height + 4
            result.Width = textSize.Width + 4
            result.Height += (Padding.Vertical + 6)
            result.Width += (Padding.Horizontal + 6)
            result.Width += splitSectionWidth
            Return result
        End Function

        Private Sub ShowContextMenuStrip()
            If skipNextOpen Then
                skipNextOpen = False
                Exit Sub
            End If
            State = PushButtonState.Pressed
            If splitMenuStrip IsNot Nothing Then
                splitMenuStrip.Show(Me, New Point(0, Height), ToolStripDropDownDirection.BelowRight)
            End If
        End Sub

        Private Sub SplitMenuStrip_Opening(sender As Object, e As CancelEventArgs)
            isSplitMenuVisible = True
        End Sub

        Private Sub SplitMenuStrip_Closing(sender As Object, e As ToolStripDropDownClosingEventArgs)
            isSplitMenuVisible = False
            SetButtonDrawState()
            If e.CloseReason = ToolStripDropDownCloseReason.AppClicked Then
                skipNextOpen = (dropDownRectangle.Contains(PointToClient(Cursor.Position))) AndAlso MouseButtons = MouseButtons.Left
            End If
        End Sub

        Private Sub SplitMenu_Popup(sender As Object, e As EventArgs)
            isSplitMenuVisible = True
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            '0x0212 == WM_EXITMENULOOP
            If m.Msg = &H212 Then
                'this message is only sent when a ContextMenu is closed (not a ContextMenuStrip)
                isSplitMenuVisible = False
                SetButtonDrawState()
            End If
            MyBase.WndProc(m)
        End Sub

        Private Sub SetButtonDrawState()
            If Bounds.Contains(Parent.PointToClient(Cursor.Position)) Then
                State = PushButtonState.Hot
            ElseIf Focused Then
                State = PushButtonState.[Default]
            ElseIf Not Enabled Then
                State = PushButtonState.Disabled
            Else
                State = PushButtonState.Normal
            End If
        End Sub
    End Class
End Class