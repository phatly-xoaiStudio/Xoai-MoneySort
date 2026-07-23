using UnityEngine;
namespace FlickSort.UI
{
    public abstract class UIBase : MonoBehaviour
    {
        protected UIManager Manager { get; private set; }

        public virtual void Init(UIManager manager) => Manager = manager;
        public virtual void SetData(params object[] data) { }

        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
    }

}
