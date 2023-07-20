Function IntArithmeticAndCasting%(Arg0%, Arg1$, Arg2#)
	Return Arg0 + Int(Arg1) + Int(Arg2)
End Function

Function SelectStatement$(Arg0%)
	Select Arg0
		Case 0
			Return "Zero"
		Case 1
			Return "One"
		Case 2
			Return "Two"
		Case 3
			Return "Three"
		Default
			Return "Something else"	
	End Select
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
