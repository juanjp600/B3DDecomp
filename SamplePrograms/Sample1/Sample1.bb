Function IntArithmeticAndCasting%(Arg0%, Arg1$, Arg2#)
	Return Arg0 + Int(Arg1) + Int(Arg2)
End Function

Function SelectStatement$(Arg0%)
	Select Arg0
		Case 0, 1
			Return "Zero or one"
		Case 2
			Return "Two"
		Case 3
			Return "Three"
		Default
			Return "Something else"	
	End Select
End Function

Function SelectStatementWithALocal$(Arg0%)
	Local Local0% = Arg0
	Select Local0
		Case 0, 1
			Return "Zero or one"
		Case 2
			Return "Two"
		Case 3
			Return "Three"
		Default
			Return "Something else"	
	End Select
End Function

Function IfElseChain$(Arg0%)
	If (Arg0 = 0) Or (Arg0 = 1) Then
		Return "Zero or one"
	ElseIf Arg0 = 2 Then
		Return "Two"
	ElseIf Arg0 = 3 Then
		Return "Three"
	Else
		Return "Something else"
	EndIf
End Function

Function IfElseChainWithWeirdLocals$(Arg0%)
	Local Local0$
	If (Arg0 = 0) Or (Arg0 = 1) Then
		Return "Zero or one"
	ElseIf Arg0 = 2 Then
		Return "Two"
	ElseIf Arg0 = 3 Then
		Return "Three"
	Else
		Return "Something else"
	EndIf
	Local Local1%
End Function

Type SomeType
	Field Field0%
	Field Field1$
	Field Field2#
	Field Field3.SomeType
End Type

Function IntArithmeticAndCastingFromSomeType.SomeType(Arg.SomeType)
	Local RetVal.SomeType = New SomeType
	RetVal\Field0 = IntArithmeticAndCasting(Arg\Field0, Arg\Field1, Arg\Field2)
	Return RetVal
End Function

Print SelectStatement(0)
Print SelectStatement(1)
Print SelectStatement(2)
Delay 1000
