using System;
using System.Reflection;

namespace BinarySerialization
{
    internal abstract class ContainerNode : Node
    {
        protected ContainerNode(Node parent, Type type)
            : base(parent, type)
        {
        }

        protected ContainerNode(Node parent, MemberInfo memberInfo) : base(parent, memberInfo)
        {
            if (FieldLengthEvaluator != null)
            {
                var source = FieldLengthEvaluator.Source;
                if (source != null)
                {
                    source.Bindings.Add(new Binding(() =>
                    {
                        var nullStream = new NullStream();
                        var streamKeeper = new StreamKeeper(nullStream);
                        Serialize(streamKeeper);
                        return streamKeeper.RelativePosition;
                    }));
                }
            }
        }

        public override object BoundValue
        {
            get { throw new BindingException("Cannot bind to a reference type."); }
        }

        protected Node GenerateChild(Type type)
        {
            Node node;
            if (type.IsPrimitive || type == typeof(string) || type == typeof(byte[]))
                node = new ValueNode(this, type);
            else if (type.IsArray)
                node = new ArrayNode(this, type);
            else if (type.IsList())
                node = new ListNode(this, type);
            else node = new ObjectNode(this, type);

            return node;
        }

        protected Node GenerateChild(MemberInfo memberInfo)
        {
            var memberType = GetMemberType(memberInfo);

            Node node;
            if (memberType.IsPrimitive || memberType == typeof(string) || memberType == typeof(byte[]))
                node = new ValueNode(this, memberInfo);
            else if(memberType.IsArray)
                node = new ArrayNode(this, memberInfo);
            else if(memberType.IsList())
                node = new ListNode(this, memberInfo);
            else node = new ObjectNode(this, memberInfo);

            return node;
        }

        protected static Type GetMemberType(MemberInfo memberInfo)
        {
            var propertyInfo = memberInfo as PropertyInfo;
            var fieldInfo = memberInfo as FieldInfo;
            
            if (propertyInfo != null)
            {
                return propertyInfo.PropertyType;
            }

            if (fieldInfo != null)
            {
                return fieldInfo.FieldType;
            }

            throw new NotSupportedException(string.Format("{0} not supported", memberInfo.GetType().Name));
        }

        protected static bool ShouldTerminate(StreamLimiter stream)
        {
            if (stream.IsAtLimit)
                return true;

            return stream.CanSeek && stream.Position >= stream.Length;
        }
    }
}