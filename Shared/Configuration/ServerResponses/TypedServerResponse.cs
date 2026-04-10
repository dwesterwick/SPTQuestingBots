using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Configuration.ServerResponses
{
    public class TypedServerResponse<T> : ServerResponse where T : class
    {
        private Type type;

        public TypedServerResponse() : base()
        {
            type = typeof(T);
        }

        public TypedServerResponse(T data) : base(data)
        {
            type = typeof(T);
        }

        public T? GetData()
        {
            return Data as T;
        }
    }
}
