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

    /// <summary>
    /// An act is over and its applause has paid for cards: the world stands while the program offers them, until
    /// <see cref="Simulation.Pick"/> or the end of the program's time. An act that picked up nothing has no program.
    /// </summary>
    Program,

    /// <summary>
    /// An act is over, and its card taken if it earned one, and the next has not begun: the world stands until
    /// <see cref="Simulation.GoOn"/>.
    /// </summary>
    BetweenActs,

    /// <summary>The last act was played to its end: the performance is over, in a standing ovation.</summary>
    Ovation,

    /// <summary>The show has closed, in an act: the performance is over.</summary>
    Closed,
}
