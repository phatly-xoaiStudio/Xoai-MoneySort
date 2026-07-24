using UnityEngine;

namespace FlickSort.UI
{
    public abstract class UIBase : MonoBehaviour
    {
        [SerializeField] private UIAnimation _animation;

        protected UIManager Manager { get; private set; }

        public virtual void Init(UIManager manager) => Manager = manager;
        public virtual void SetData(params object[] data) { }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            _animation?.PlayShow();
        }

        public virtual void Hide()
        {
            if (_animation != null && gameObject.activeSelf)
                _animation.PlayHide(() => gameObject.SetActive(false));
            else
                gameObject.SetActive(false);
        }

        public void HideImmediate()
        {
            _animation?.SetHiddenImmediate();
            gameObject.SetActive(false);
        }
    }
}
