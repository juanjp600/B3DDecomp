Type Child1
End Type

Type Child2
End Type

Type Base
	Field ChildPtr%
End Type

Local myBase.Base = New Base
Local myChild1.Child1 = New Child1
myBase\ChildPtr = Handle myChild1
Local deref.Child1 = Object.Child1(myBase\ChildPtr)
