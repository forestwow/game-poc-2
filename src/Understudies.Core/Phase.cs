namespace Understudies.Core;

/// <summary>Where a performance stands.</summary>
public enum Phase
{
    /// <summary>An act is being played.</summary>
    Act,

    /// <summary>An act is over and the next has not begun: the world stands until <see cref="Simulation.GoOn"/>.</summary>
    BetweenActs,

    /// <summary>The last act was played to its end: the performance is over, in a standing ovation.</summary>
    Ovation,

    /// <summary>The show has closed, in an act: the performance is over.</summary>
    Closed,
}
