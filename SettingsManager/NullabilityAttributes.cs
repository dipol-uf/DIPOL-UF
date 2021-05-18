#if NETSTANDARD2_0 
namespace System.Diagnostics.CodeAnalysis
 {

     [AttributeUsage(
         AttributeTargets.Field | AttributeTargets.Parameter |
         AttributeTargets.Property | AttributeTargets.ReturnValue
     )]

     internal sealed class MaybeNullAttribute : Attribute
     {

         public MaybeNullAttribute()
         {
         }
     }
     
     [AttributeUsage(AttributeTargets.Parameter)]
     internal sealed class MaybeNullWhenAttribute : Attribute
     {
         public bool ReturnValue { get; }

         public MaybeNullWhenAttribute(bool returnValue)
         {
             ReturnValue = returnValue;
         }
     }
 }
#endif
