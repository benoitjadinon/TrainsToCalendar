using System;
using Android.App;
using Android.OS;
using System.Linq.Expressions;

namespace Trains2Calendar
{
	public static class BundleExtensions
	{
		public static T Get<T> (this Bundle bundle, string propName, T defaultValue = default(T))
		{			
			if (bundle != null && bundle.ContainsKey (propName)) {
				if (typeof(T) == typeof(int))
					return (T)Convert.ChangeType (bundle.GetInt (propName, (int)Convert.ChangeType (defaultValue, typeof(int))), typeof(T));
			}
			return defaultValue;
		}
		public static T Get<T> (this Bundle bundle, Expression<Func<T>> property, T defaultValue = default(T))
		{
			MemberExpression member = property.Body as MemberExpression;
			return bundle.Get(member.Member.Name, defaultValue);
		}

		public static Bundle Put<T> (this Bundle bundle, string propName, T value)
		{
			if (bundle != null) {
				if (typeof(T) == typeof(int))
					bundle.PutInt (propName, (int)Convert.ChangeType (value, typeof(int)));
				//TODO: support all types
			}
			return bundle;
		}
		public static Bundle Put<T> (this Bundle bundle, Expression<Func<T>> property)
		{
			MemberExpression member = property.Body as MemberExpression;
			bundle.Put(member.Member.Name, property.Compile()());
			return bundle;
		}
	}
}

