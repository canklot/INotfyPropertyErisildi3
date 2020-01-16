using System;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;

namespace INotfyPropertyErisildi3
{
    public delegate void PropertyErisildiEventHandler(object sender, PropertyChangedEventArgs e); //EventHandler'ın tanımlanması
    public interface INotifyPropertyErisildi
    {
        event PropertyErisildiEventHandler PropertyErisildi;
    }




    [ImplementPropertyErisildi]
    public class Person
    {
        public virtual string Ad { get; set; }
        public virtual string Soyad { get; set; }
        public virtual string TelNo { get; set; }
        public virtual string Adres { get; set; }
        [DontNotifyPropertyErisildi]
        public virtual string TcNo { get; set; }
    }






    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ImplementPropertyErisildiAttribute : Attribute
    {

    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public class NotifyPropertyErisildiAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
    public class DontNotifyPropertyErisildiAttribute : Attribute
    {
    }





    class Program
    {
        static void Main(string[] args)
        {
            Person personTest = CreateType<Person>(); //Person nesnesinden instance oluşturulması

            (personTest as INotifyPropertyErisildi).PropertyErisildi += PatronHaberimOldu;  //Patronun EventHandler'a abone olması 

            personTest.Ad = "Mustafa";
            personTest.Soyad = "Candan";
            personTest.TcNo = "26758277566";  //personTest kişisine değer atama
            personTest.TelNo = "5518300255";
            personTest.Adres = "Mars";

            Console.WriteLine("  Personel Adi :" + personTest.Ad + "\n");
            Console.WriteLine("  Personel SoyAdi :" + personTest.Soyad + "\n");  //Erişimi tetiklemek için ekrana basılması
            Console.WriteLine("  Personel Tc: " + personTest.TcNo + "\n");
            Console.WriteLine("  Personel Tel NO: " + personTest.TelNo + "\n");
            Console.WriteLine("  Personel Adress: " + personTest.Adres + "\n");

        }







        private static void PatronHaberimOldu(object sender, PropertyChangedEventArgs e)  //PatronHaberimOldu fonksiyonu'nun tanımlanması 
        {
            Console.WriteLine("{0} itemine erisim yakalandi ", e.PropertyName);
        }






        public static T CreateType<T>() where T : class           //Generic class'ın tanımlanması                                           
        {
            //var assemblyName = "MyProxies";
            var name = new AssemblyName("MyProxies");
            var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            var ModuleYaratici = assembly.DefineDynamicModule("MyProxies");
            var TipYaratici = ModuleYaratici.DefineType("MyPersonProxy", TypeAttributes.Class | TypeAttributes.Public, typeof(T));

            bool MyClassAttribute = typeof(T).GetCustomAttribute<ImplementPropertyErisildiAttribute>() != null;     //Girdi olarak alınan sınıfın attribute'ünün kontrol edilmesi                              
            bool NotifyPropertyErisildiAttribute = typeof(T).GetProperties().Where(x => x.GetCustomAttribute<NotifyPropertyErisildiAttribute>() != null).Any();  //Sınıfın Property'lerinin attribute'lerinin kontrol edilmesi
            bool DoNotNotifyPropertyErisildiAttribute = false;

            if (MyClassAttribute == false && NotifyPropertyErisildiAttribute == false) //Hem sınıfta hemde barındırdığı property'lerde bizim attribute yok ise
                return Activator.CreateInstance<T>();                                    // sınıfa kod enjekte edilmeyeceği için sınıfın işlem görmeden yaratılması

            TipYaratici.AddInterfaceImplementation(typeof(INotifyPropertyErisildi)); //Interface'in uygulanması
            TipYaratici.DefineDefaultConstructor(MethodAttributes.Public); //Constructor'ün tanımlanması

            //Event oluşturmak için bir EventBuilder oluşturuyoruz tipi "PropertyGetEventHandler"
            EventBuilder eventBuilder = TipYaratici.DefineEvent("PropertyErisildi", EventAttributes.ReservedMask | EventAttributes.RTSpecialName | EventAttributes.SpecialName, typeof(PropertyErisildiEventHandler));
            //Bir FieldBuilder oluşturuyluyor PropertyGetEventHandler tipinde.
            FieldBuilder eventField = TipYaratici.DefineField("PropertyErisildi", typeof(PropertyErisildiEventHandler), FieldAttributes.Private);


            //event in add methodu
            var eventAddMethod = TipYaratici.DefineMethod("add_PropertyErisildi",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis, typeof(void), new[] { typeof(PropertyErisildiEventHandler) });
            var addEventIL = eventAddMethod.GetILGenerator();
            var combine = typeof(Delegate).GetMethod("Combine", new[] { typeof(Delegate), typeof(Delegate) });
            addEventIL.Emit(OpCodes.Ldarg_0);
            addEventIL.Emit(OpCodes.Ldarg_0);
            addEventIL.Emit(OpCodes.Ldfld, eventField);
            addEventIL.Emit(OpCodes.Ldarg_1);
            addEventIL.Emit(OpCodes.Call, combine);
            addEventIL.Emit(OpCodes.Castclass, typeof(PropertyErisildiEventHandler));
            addEventIL.Emit(OpCodes.Stfld, eventField);
            addEventIL.Emit(OpCodes.Ret);
            eventBuilder.SetAddOnMethod(eventAddMethod);

            //event in remove methodu
            var removeMethod = TipYaratici.DefineMethod("remove_PropertyErisildi",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis, typeof(void), new[] { typeof(PropertyErisildiEventHandler) });
            var remove = typeof(Delegate).GetMethod("Remove", new[] { typeof(Delegate), typeof(Delegate) });
            var removeEventIL = removeMethod.GetILGenerator();
            removeEventIL.Emit(OpCodes.Ldarg_0);
            removeEventIL.Emit(OpCodes.Ldfld, eventField);
            removeEventIL.Emit(OpCodes.Ldarg_1);
            removeEventIL.Emit(OpCodes.Call, remove);
            removeEventIL.Emit(OpCodes.Castclass, typeof(PropertyErisildiEventHandler));
            removeEventIL.Emit(OpCodes.Stfld, eventField);
            removeEventIL.Emit(OpCodes.Ret);
            eventBuilder.SetRemoveOnMethod(removeMethod);

            //Before get
            var methodBuilder = TipYaratici.DefineMethod("BeforeGet", MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot, typeof(void), new[] { typeof(string) });
            var generator = methodBuilder.GetILGenerator();
            var returnLabel = generator.DefineLabel();
            var eventArgsCtor = typeof(PropertyChangedEventArgs).GetConstructor(new[] { typeof(string) });
            generator.DeclareLocal(typeof(PropertyErisildiEventHandler));
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, eventField);
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Brfalse, returnLabel);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Newobj, eventArgsCtor);
            generator.Emit(OpCodes.Callvirt, typeof(PropertyChangedEventHandler).GetMethod("Invoke"));
            generator.MarkLabel(returnLabel);
            generator.Emit(OpCodes.Ret);
            eventBuilder.SetRaiseMethod(methodBuilder);

            foreach (var item in typeof(Person).GetProperties())
            {
                if (MyClassAttribute == true)
                {
                    DoNotNotifyPropertyErisildiAttribute = item.GetCustomAttribute<DontNotifyPropertyErisildiAttribute>() != null;
                    if (DoNotNotifyPropertyErisildiAttribute == true)
                        continue;
                }
                else
                {
                    NotifyPropertyErisildiAttribute = item.GetCustomAttribute<NotifyPropertyErisildiAttribute>() != null;
                    if (NotifyPropertyErisildiAttribute == false)
                        continue;
                }

                PropertyBuilder PropertyYaratici = TipYaratici.DefineProperty(item.Name, PropertyAttributes.None, item.PropertyType, Type.EmptyTypes);
                MethodBuilder getMethod = TipYaratici.DefineMethod("get_" + item.Name, MethodAttributes.Public | MethodAttributes.Virtual, item.PropertyType, Type.EmptyTypes);
                ILGenerator genericIL = getMethod.GetILGenerator();
                genericIL.Emit(OpCodes.Ldarg_0);
                genericIL.Emit(OpCodes.Ldstr, item.Name);
                genericIL.Emit(OpCodes.Call, methodBuilder);
                genericIL.Emit(OpCodes.Ldarg_0);
                genericIL.Emit(OpCodes.Call, item.GetGetMethod());
                genericIL.Emit(OpCodes.Ret);
                PropertyYaratici.SetGetMethod(getMethod);

                MethodBuilder setMethod = TipYaratici.DefineMethod("set_" + item.Name, MethodAttributes.Public | MethodAttributes.Virtual, null, new Type[] { item.PropertyType });
                genericIL = setMethod.GetILGenerator();
                genericIL.Emit(OpCodes.Ldarg_0);
                genericIL.Emit(OpCodes.Ldarg_1);
                genericIL.Emit(OpCodes.Call, item.GetSetMethod());
                genericIL.Emit(OpCodes.Ret);
                PropertyYaratici.SetSetMethod(setMethod);
            }

            return Activator.CreateInstance(TipYaratici.CreateType()) as T;
        }
    }

   

    
   

}
