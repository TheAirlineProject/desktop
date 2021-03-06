﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using TheAirline.Infrastructure;

namespace TheAirline.Models
{
    public abstract class BaseModel : ISerializable
    {
        protected readonly int Version;

        protected BaseModel()
        {
        }

        protected BaseModel(SerializationInfo info, StreamingContext context)
        {
            Version = info.GetInt16("version");

            IEnumerable<FieldInfo> fields =
                GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute(typeof (Versioning)) != null);

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    GetType()
                        .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                        .Where(p => p.GetCustomAttribute(typeof (Versioning)) != null));

            IEnumerable<MemberInfo> propsAndFields = props.Cast<MemberInfo>().Union(fields).ToList();

            foreach (SerializationEntry entry in info)
            {
                MemberInfo prop =
                    propsAndFields.FirstOrDefault(
                        p => ((Versioning) p.GetCustomAttribute(typeof (Versioning))).Name == entry.Name);

                if (prop != null)
                {
                    var fieldInfo = prop as FieldInfo;
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(this, entry.Value);
                    }

                    else
                    {
                        ((PropertyInfo) prop).SetValue(this, entry.Value);
                    }
                }
            }

            IEnumerable<MemberInfo> notSetProps =
                propsAndFields.Where(p => ((Versioning) p.GetCustomAttribute(typeof (Versioning))).Version > Version);

            foreach (MemberInfo notSet in notSetProps)
            {
                var ver = (Versioning) notSet.GetCustomAttribute(typeof (Versioning));

                if (ver.AutoGenerated)
                {
                    var set = notSet as FieldInfo;
                    if (set != null)
                    {
                        set.SetValue(this, ver.DefaultValue);
                    }
                    else
                    {
                        ((PropertyInfo) notSet).SetValue(this, ver.DefaultValue);
                    }
                }
            }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Type myType = GetType();

            IEnumerable<FieldInfo> fields =
                myType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                      .Where(p => p.GetCustomAttribute(typeof (Versioning)) != null);

            IList<PropertyInfo> props =
                new List<PropertyInfo>(
                    myType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                          .Where(p => p.GetCustomAttribute(typeof (Versioning)) != null));

            IEnumerable<MemberInfo> propsAndFields = props.Cast<MemberInfo>().Union(fields);

            foreach (MemberInfo member in propsAndFields)
            {
                var fieldInfo = member as FieldInfo;
                object propValue = fieldInfo != null ? fieldInfo.GetValue(this) : ((PropertyInfo) member).GetValue(this, null);

                var att = (Versioning) member.GetCustomAttribute(typeof (Versioning));

                info.AddValue(att.Name, propValue);
            }
        }
    }
}