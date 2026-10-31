using System;
using EchoesOfAscent.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoesOfAscent.Input
{
    public sealed class SwipeInputProvider : MonoBehaviour, ISwipeInputProvider
    {
        public event Action<SwipeData> OnSwipeUpdated;
        public event Action<SwipeData> OnSwipeReleased;
        public event Action OnSwipeCancelled;

        [SerializeField] float minSwipePixels = 25f;
        [SerializeField] float maxReferencePixels = 280f;

        public bool IsDragging { get; private set; }

        Vector2 startScreen;
        float startTime;
        bool usingMouse;

        public float MinSwipePixels => minSwipePixels;

        void Update()
        {
            if (TryHandleMouse())
                return;

            TryHandleTouch();
        }

        bool TryHandleMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;

            Vector2 position = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Begin(position, true);
                return true;
            }

            if (!IsDragging || !usingMouse)
                return false;

            if (mouse.leftButton.isPressed)
            {
                Drag(position);
                return true;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                End(position);
                return true;
            }

            return false;
        }

        void TryHandleTouch()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
                return;

            var touch = touchscreen.primaryTouch;
            Vector2 position = touch.position.ReadValue();

            if (touch.press.wasPressedThisFrame)
                Begin(position, false);
            else if (IsDragging && !usingMouse && touch.press.isPressed)
                Drag(position);
            else if (IsDragging && !usingMouse && touch.press.wasReleasedThisFrame)
                End(position);
        }

        void Begin(Vector2 screenPosition, bool fromMouse)
        {
            usingMouse = fromMouse;
            startScreen = screenPosition;
            startTime = Time.unscaledTime;
            IsDragging = true;
            EmitUpdated(screenPosition);
        }

        void Drag(Vector2 screenPosition)
        {
            EmitUpdated(screenPosition);
        }

        void End(Vector2 screenPosition)
        {
            var swipe = BuildSwipe(screenPosition);
            IsDragging = false;

            if (swipe.IsValid(minSwipePixels))
                OnSwipeReleased?.Invoke(swipe);
            else
                OnSwipeCancelled?.Invoke();
        }

        void EmitUpdated(Vector2 screenPosition)
        {
            OnSwipeUpdated?.Invoke(BuildSwipe(screenPosition));
        }

        SwipeData BuildSwipe(Vector2 screenPosition)
        {
            return new SwipeData(startScreen, screenPosition, maxReferencePixels, Time.unscaledTime - startTime);
        }

        /// <summary>
        /// Injects a swipe update for automated tests without hardware input.
        /// </summary>
        public void InjectSwipeUpdate(SwipeData swipe)
        {
            IsDragging = true;
            OnSwipeUpdated?.Invoke(swipe);
        }

        /// <summary>
        /// Injects a swipe release for automated tests without hardware input.
        /// </summary>
        public void InjectSwipeRelease(SwipeData swipe)
        {
            IsDragging = false;

            if (swipe.IsValid(minSwipePixels))
                OnSwipeReleased?.Invoke(swipe);
            else
                OnSwipeCancelled?.Invoke();
        }
    }
}
