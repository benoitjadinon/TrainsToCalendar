using System;
using Android.App;
using Android.OS;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Java.Lang;
using Android.Runtime;
using System.Runtime.Serialization;

namespace Trains2Calendar
{
	public static class BundleExtensions
	{
		const string jSONSERIALIZED = "__JSONSERIALIZED__";

		public static T GetValue<T> (this Bundle bundle, string propName, T defaultValue = default(T))
		{
			if (bundle != null) {
				bool hasConverted = false;

				if (bundle.ContainsKey (propName)) {
					if (typeof(T) == typeof(bool))
						return DoConvert<T> (bundle.GetBoolean (propName, DoConvert<bool> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(bool[]))
						return DoConvert<T> (bundle.GetBooleanArray (propName), out hasConverted);

					if (typeof(T) == typeof(Bundle))
						return DoConvert<T> (bundle.GetBundle (propName), out hasConverted);

					if (typeof(T) == typeof(sbyte))
						return DoConvert<T> (bundle.GetByte (propName, DoConvert<sbyte> (defaultValue, out hasConverted)), out hasConverted);
					//if (typeof(T) == typeof(Byte))
					//	return DoConvert<T> (bundle.GetByte (propName, DoConvert<Java.Lang.Byte> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(byte[]))
						return DoConvert<T> (bundle.GetByteArray (propName), out hasConverted);

					if (typeof(T) == typeof(char))
						return DoConvert<T> (bundle.GetChar (propName, DoConvert<char> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(char[]))
						return DoConvert<T> (bundle.GetCharArray (propName), out hasConverted);

					/*
					if (typeof(T) == typeof(CharSequence)) 
						return DoConvert<T> (bundle.GetChar (propName, DoConvert<char> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(char[])) 
						return DoConvert<T> (bundle.GetCharArray (propName, DoConvert<char[]> (defaultValue, out hasConverted)), out hasConverted);
						*/

					if (typeof(T) == typeof(double))
						return DoConvert<T> (bundle.GetDouble (propName, DoConvert<double> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(double[]))
						return DoConvert<T> (bundle.GetDoubleArray (propName), out hasConverted);

					if (typeof(T) == typeof(float))
						return DoConvert<T> (bundle.GetFloat (propName, DoConvert<float> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(float[]))
						return DoConvert<T> (bundle.GetFloatArray (propName), out hasConverted);

					if (typeof(T) == typeof(int))
						return DoConvert<T> (bundle.GetInt (propName, DoConvert<int> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(int[]))
						return DoConvert<T> (bundle.GetIntArray (propName), out hasConverted);
					if (typeof(T) == typeof(IList<int>))
						return DoConvert<T> (bundle.GetIntegerArrayList (propName), out hasConverted);

					if (typeof(T) == typeof(long))
						return DoConvert<T> (bundle.GetLong (propName, DoConvert<long> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(long[]))
						return DoConvert<T> (bundle.GetLongArray (propName), out hasConverted);

					if (typeof(T) == typeof(IParcelable))
						return DoConvert<T> (bundle.GetParcelable (propName), out hasConverted);
					if (typeof(T) == typeof(IParcelable[]))
						return DoConvert<T> (bundle.GetParcelableArray (propName), out hasConverted);

					if (typeof(T) == typeof(ISerializable))
						return DoConvert<T> (bundle.GetSerializable (propName), out hasConverted);

					if (typeof(T) == typeof(short))
						return DoConvert<T> (bundle.GetShort (propName, DoConvert<short> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(short[]))
						return DoConvert<T> (bundle.GetShortArray (propName), out hasConverted);
					
					if (typeof(T) == typeof(string))
						return DoConvert<T> (bundle.GetString (propName, DoConvert<string> (defaultValue, out hasConverted)), out hasConverted);
					if (typeof(T) == typeof(string[]))
						return DoConvert<T> (bundle.GetStringArray (propName), out hasConverted);
					if (typeof(T) == typeof(IList<string>))
						return DoConvert<T> (bundle.GetStringArrayList (propName), out hasConverted);
					/*
						if (typeof(T) == typeof(Integer[])) 
							return DoConvert<T> (bundle.GetIntegerArrayList (propName, DoConvert<Integer[]> (defaultValue, out hasConverted)), out hasConverted);

					*/

				} else if (bundle.ContainsKey (jSONSERIALIZED + propName)) {
					//return jsonconvert.from(bundle.GetString(jSONSERIALIZED+propName));
				}
			}

			return default(T);
		}

		public static T GetValue<T> (this Bundle bundle, Expression<Func<T>> property, T defaultValue = default(T))
		{
			MemberExpression member = property.Body as MemberExpression;
			return GetValue(bundle, member.Member.Name, defaultValue);
		}


		public static Bundle PutValue<T> (this Bundle bundle, string propName, T value) 
		{
			if (bundle == null) 
				bundle = new Bundle();

			bool hasConverted = false;

			if (typeof(T) == typeof(Bundle)) //Fill(bundle, value, (Bundle b) => b.PutAll);
				bundle.PutAll (DoConvert<Bundle> (value, out hasConverted));

			else if (typeof(T) == typeof(string)) //Fill(bundle, value, (Bundle b) => b.PutString);
				bundle.PutString (propName, DoConvert<string> (value, out hasConverted));

			else if (typeof(T) == typeof(int)) //Fill(bundle, value, (Bundle b) => b.PutInt);
				bundle.PutInt (propName, DoConvert<int> (value, out hasConverted));

			// TEST
			//Fill (bundle, propName, false, b => b.PutBoolean );//, (Bundle b) => b.GetBoolean);

			if (hasConverted == false) {
				throw new NotSupportedException(string.Format("type {0} not supported for param {1}", typeof(T).Name, propName));
				//bundle.PutString(jSONSERIALIZED + propName, Json.Convert(value));
			}

			return bundle;
		}

		public static Bundle PutValue<T> (this Bundle bundle, Expression<Func<T>> property)
		{
			MemberExpression member = property.Body as MemberExpression;
			bundle.PutValue(member.Member.Name, property.Compile()());
			return bundle;
		}


		static void Fill<T> (Bundle bundle, string key, T value, Expression<Func<Bundle, Action<string, T>>> put)//, Expression<Func<string, T>> get = null)
		{
			put.Compile().Invoke(bundle).Invoke(key, value);
		}
		/*
		static TB Fill<TB,T> (Func<T, TB> put, Action<T> get, T value)
		{
			put(value);
		}*/


		public static TConvertType DoConvert<TConvertType>(object convertValue, out bool hasConverted)
		{
		    hasConverted = false;
		    var converted = default(TConvertType);
		    try
		    {
		        converted = (TConvertType) Convert.ChangeType(convertValue, typeof(TConvertType));
		        hasConverted = true;
		    }
		    catch (InvalidCastException)
		    {
		    }
		    catch (ArgumentNullException)
		    {
		    }
		    catch (FormatException)
		    {
		    }
		    catch (OverflowException)
		    {
		    }

		    return converted;
		}
	}
}

