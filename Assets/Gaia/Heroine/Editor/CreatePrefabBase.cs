using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2;
using Kit2.Tasks;
using System.IO;
using UnityEditor;
using UnityEngine.Animations;
namespace Gaia
{
    public abstract class CreatePrefabBase : MyTaskWithState
	{
		protected Transform _FindChildren(Transform parent, string name)
		{
			var queue = new Queue<Transform>();
			queue.Enqueue(parent);
			while (queue.Count > 0)
			{
				var current = queue.Dequeue();
				if (current.name.Equals(name, System.StringComparison.InvariantCultureIgnoreCase))
					return current;
				if (current.childCount <= 0)
					continue;
				for (int i = 0; i < current.childCount; ++i)
					queue.Enqueue(current.GetChild(i));
			}
			return null;
		}

		protected Transform _FindOrCreateChild(Transform root, string cName)
		{
			var target = root.Find(cName);
			if (target == null)
			{
				var go = new GameObject(cName);
				target = go.transform;
				target.SetParent(root, false);
				target.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
				target.localScale = Vector3.one;
			}
			target.SetParent(root, false);
			target.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			target.localScale = Vector3.one;
			return target;
		}

		protected bool _TryFindChild(Transform root, System.Func<Transform, bool> func, out Transform result)
		{
			result = null;
			if (func == null)
				return false;

			var queue = new Queue<Transform>();
			queue.Enqueue(root);

			while (queue.TryDequeue(out var obj))
			{
				if (!func.Invoke(obj))
				{
					for (int i = 0; i < obj.childCount; ++i)
						queue.Enqueue(obj.GetChild(i));
					continue;
				}
				result = obj;
				return true;
			}
			return false;
		}

		protected void _TryReparent(string[] arr, Transform searchRoot)
		{
			if (arr.Length < 2)
			{
				throw new System.Exception("Invalid data, must have at least 2 elements in array");
			}
            if (searchRoot == null)
            {
				throw new System.Exception("SearchRoot cannot be null.");
            }

            var root = searchRoot;
			if (!_TryFindChild(root, (t) => t.name.Equals(arr[0], System.StringComparison.InvariantCultureIgnoreCase), out var parent))
			{
				throw new System.Exception($"Failed to find parent: {arr[0]}");
			}

			for (int i = 1; i < arr.Length; ++i)
			{
				if (!_TryFindChild(root, (t) => t.name.Equals(arr[i], System.StringComparison.OrdinalIgnoreCase), out var obj))
				{
					throw new System.Exception($"Failed to find: {arr[i]}");
				}
				if (obj)
					obj.SetParent(parent, true);
			}
		}

		protected void _TryRemoveChildrens(Transform root, System.Func<Transform, bool> func)
		{
			var queue = new Queue<Transform>();
			queue.Enqueue(root);

			while (queue.TryDequeue(out var obj))
			{
				if (func.Invoke(obj))
				{
					GameObject.DestroyImmediate(obj.gameObject);
					continue; // destroy and skip children
				}

				for (int i = 0; i < obj.childCount; ++i)
					queue.Enqueue(obj.GetChild(i));
			}
		}

		protected void _WritePrefabVariant(string folder, string fileName, GameObject go, bool destroyGoAfterward)
		{
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
			if (go == null)
			{
				Debug.LogError($"{GetType().Name} failed.");
				return;
			}
			var path = $"{folder}/{fileName}.prefab";
			var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
			GameObject.DestroyImmediate(go);
			Debug.Log($"{GetType().Name} completed.");
		}

		protected void _ParentConstraint(Transform root, string targetName, string parentName, float weight01)
		{
			var target = _FindChildren(root, targetName);
			var parent = _FindChildren(root, parentName);
			if (target == null || parent == null)
			{
				Debug.LogWarning($"Fail to setup parent constraint: {targetName} -> {parentName}");
				return;
			}
			_ParentConstraint(target, parent, weight01);
		}

		protected void _ParentConstraint(Transform target, Transform parent, float weight01)
		{
			if (target == null || parent == null)
				return;
			var pc = target.GetOrAddComponent<ParentConstraint>();
			var data = new ConstraintSource() { sourceTransform = parent, weight = 1f };
			if (pc.sourceCount == 0)
				pc.AddSource(data);
			else
				pc.SetSource(0, data);

			var posOffset = parent.InverseTransformPoint(target.position);
			pc.SetTranslationOffset(0, posOffset);
			var localFwd = parent.InverseTransformDirection(target.forward);
			var localUp = parent.InverseTransformDirection(target.up);
			var rotateOffset = Quaternion.LookRotation(localFwd, localUp).eulerAngles;
			pc.SetRotationOffset(0, rotateOffset);
			pc.weight = weight01;
			pc.constraintActive = true;
		}
	}
}