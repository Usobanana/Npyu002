using System.Collections.Generic;

namespace ActionGame.AI
{
    public enum NodeState { Success, Failure, Running }

    /// <summary>Behavior Tree ノードの基底クラス</summary>
    public abstract class BTNode
    {
        public NodeState State { get; protected set; } = NodeState.Running;
        public abstract NodeState Evaluate();
    }

    /// <summary>複数の子ノードを持つ複合ノード</summary>
    public abstract class BTComposite : BTNode
    {
        protected readonly List<BTNode> children = new();
        public void AddChild(BTNode child) => children.Add(child);
    }

    /// <summary>
    /// Selector: 子ノードを順に評価し、最初に Success/Running を返した時点でそれを返す。
    /// 全て Failure の場合のみ Failure を返す。
    /// </summary>
    public class BTSelector : BTComposite
    {
        public override NodeState Evaluate()
        {
            foreach (var child in children)
            {
                var result = child.Evaluate();
                if (result != NodeState.Failure)
                    return State = result;
            }
            return State = NodeState.Failure;
        }
    }

    /// <summary>
    /// Sequence: 子ノードを順に評価し、最初に Failure/Running を返した時点でそれを返す。
    /// 全て Success の場合のみ Success を返す。
    /// </summary>
    public class BTSequence : BTComposite
    {
        public override NodeState Evaluate()
        {
            foreach (var child in children)
            {
                var result = child.Evaluate();
                if (result != NodeState.Success)
                    return State = result;
            }
            return State = NodeState.Success;
        }
    }

    /// <summary>
    /// 条件ラムダだけで作れるリーフノード。
    /// true → Success / false → Failure
    /// </summary>
    public class BTCondition : BTNode
    {
        readonly System.Func<bool> condition;
        public BTCondition(System.Func<bool> c) { condition = c; }
        public override NodeState Evaluate() =>
            State = condition() ? NodeState.Success : NodeState.Failure;
    }

    /// <summary>
    /// アクションラムダだけで作れるリーフノード。
    /// ラムダ内で NodeState を返す。
    /// </summary>
    public class BTAction : BTNode
    {
        readonly System.Func<NodeState> action;
        public BTAction(System.Func<NodeState> a) { action = a; }
        public override NodeState Evaluate() => State = action();
    }

    /// <summary>BT ノード間で共有するデータストア</summary>
    public class BTBlackboard
    {
        private readonly Dictionary<string, object> data = new();

        public void Set<T>(string key, T value) => data[key] = value;

        public T Get<T>(string key, T defaultValue = default)
        {
            return data.TryGetValue(key, out var val) ? (T)val : defaultValue;
        }

        public bool Has(string key) => data.ContainsKey(key);
    }
}
