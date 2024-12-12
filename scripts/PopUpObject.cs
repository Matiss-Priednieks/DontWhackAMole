// using Godot;
// using Godot.Collections;
// using System;
// using System.Xml.Schema;
// public abstract class PopUpObject : Area3D
// {
//     public Vector3[] Holes { get; protected set; }
//     public Vector3 Position { get; protected set; }
//     public bool Down { get; protected set; }
//     public bool Ready { get; protected set; }
//     public float PopSpeed { get; protected set; }

//     protected Node3D MeshNode;
//     protected Tween TweenNode;

//     public override void _Ready()
//     {
//         MeshNode = GetNode<Node3D>("%Mesh");
//         TweenNode = GetTree().CreateTween();
//     }

//     public virtual void PopOut()
//     {
//         ChangeRandomHole();
//         Vector3 newHole = Holes[randomHoleIndex];
//         TweenNode.TweenProperty(this, "position", new Vector3(newHole.X, 1.13f, newHole.Z), PopSpeed).SetTrans(Tween.TransitionType.Elastic);
//         Down = false;
//         Ready = false;
//     }

//     protected RandomNumberGenerator RNG = new RandomNumberGenerator();
//     public Vector3 ChangeRandomHole()
//     {
//         int randomHoleIndex = RNG.RandiRange(0, Holes.Length - 1);
//         RandomChosenHole = Holes[randomHoleIndex];
//         while (MoleRef.GetChosenHole() == RandomChosenHole)
//         {
//             randomHoleIndex = RNG.RandiRange(0, Holes.Length - 1);
//             RandomChosenHole = Holes[randomHoleIndex];
//         }
//         return RandomChosenHole;
//     }
// }