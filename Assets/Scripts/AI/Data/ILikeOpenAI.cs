using Deepseek;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AI
{
    public interface IOpenAILike
    {
        public string modelName { get; }

        public Task<string[]> ListModels();

        public Task<Conversation> Chat(MessageBase[] messages);
    }
}