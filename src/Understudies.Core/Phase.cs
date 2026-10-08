namespace Understudies.Core;

/// <summary>Where a performance stands.</summary>
public enum Phase
{
    /// <summary>
    /// The curtain is up: the moment every act begins with, in which the stage stands and only the curtain's own
    /// time goes by.
    /// </summary>
    Curtain,

    /// <summary>An act is being played.</summary>
    Act,

    /// <summary>An act is over and the next has not begun: the world stands until <see cref="Simulation.GoOn"/>.</summary>
    BetweenActs,

    /// <summary>The last act was played to its end: the performance is over, in a standing ovation.</summary>
    Ovation,

    /// <summary>The show has closed, in an act: the performance is over.</summary>
    Closed,
}
