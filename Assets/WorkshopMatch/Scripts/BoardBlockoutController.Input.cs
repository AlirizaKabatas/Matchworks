using UnityEngine;
using UnityEngine.InputSystem;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private readonly BoardPointerGesture boardPointer = new();
        private Match3BoardModel.Cell boardPointerCell;
        private bool boardPointerIsTouch;
        private readonly MapSwipeGesture mapSwipe = new();

        private void UpdateBoardPointer()
        {
            var touch = Touchscreen.current?.primaryTouch;
            bool useTouch = boardPointer.Tracking ? boardPointerIsTouch :
                touch != null && (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame);
            Vector2 point;
            bool pressed, held, released;
            if (useTouch && touch != null)
            {
                point = touch.position.ReadValue(); pressed = touch.press.wasPressedThisFrame;
                held = touch.press.isPressed; released = touch.press.wasReleasedThisFrame;
            }
            else if (!useTouch && Mouse.current != null)
            {
                point = Mouse.current.position.ReadValue(); pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed; released = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else { boardPointer.Reset(); return; }

            if (pressed)
            {
                boardPointer.Reset();
                if (PointerOverHud(point) || !TryGetPointerCell(point, out var cell) || !TutorialAllowsCell(cell)) return;
                boardPointerCell = cell;
                boardPointerIsTouch = useTouch;
                boardPointer.Begin(point);
                ResetHintTimer();
            }
            float threshold = Mathf.Max(8f, GameplayLayout(GetPortraitRect()).Aperture.width / boardWidth * .30f);
            var action = boardPointer.Sample(point, held, released, threshold, out int dx, out int dy);
            if (action == BoardGestureAction.None || PointerOverHud(point)) return;
            if (action == BoardGestureAction.Tap)
            {
                if (TryGetPointerCell(point, out var cell) && cell.Equals(boardPointerCell)) HandleCellTap(cell);
                return;
            }
            if (activeTool.HasValue || model.GetContent(boardPointerCell) != CellContent.Candy) return;
            var target = new Match3BoardModel.Cell(boardPointerCell.X + dx, boardPointerCell.Y + dy);
            if (target.X < 0 || target.X >= boardWidth || target.Y < 0 || target.Y >= boardHeight ||
                model.GetContent(target) != CellContent.Candy ||
                !TutorialAllowsCell(boardPointerCell) || !TutorialAllowsCell(target)) return;
            ClearSelection();
            Select(boardPointerCell);
            HandleCellTap(target);
        }

        private bool TryGetPointerCell(Vector2 screenPosition, out Match3BoardModel.Cell cell)
        {
            cell = default;
            return boardCamera != null && Physics.Raycast(boardCamera.ScreenPointToRay(screenPosition), out var hit, 30f) &&
                coordinates.TryGetValue(hit.collider.transform, out cell);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused) return;
            boardPointer.Reset(); mapSwipe.Reset();
            mapPointerDown = mapDragging = false;
        }
    }
}
