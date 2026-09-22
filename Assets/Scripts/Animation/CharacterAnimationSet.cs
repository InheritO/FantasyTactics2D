using UnityEngine;

public enum FacingDirection { Up, Down, Left, Right }


[CreateAssetMenu(fileName = "CharacterAnimationSet", menuName = "Strategy/Visuals/Character Animation Set")]
public class CharacterAnimationSet : ScriptableObject
{
    [System.Serializable]
    public class DirectionalFrames
    {
        public Sprite[] up;
        public Sprite[] down;
        public Sprite[] left;
        public Sprite[] right;

        public Sprite[] Get(FacingDirection dir) => dir switch
        {
            FacingDirection.Up => up,
            FacingDirection.Down => down,
            FacingDirection.Left => left,
            FacingDirection.Right => right,
            _ => down
        };
    }

    public DirectionalFrames idle;
    public DirectionalFrames combatIdle;
    public DirectionalFrames walk;
    public DirectionalFrames slash;
    public DirectionalFrames thrust;
    public DirectionalFrames shoot;
}