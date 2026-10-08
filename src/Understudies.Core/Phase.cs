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
    /// An act stands for an encore (plan decision 26): its applause has paid for one, and the world and the act's
    /// own time stand while three self cards are offered, until <see cref="Simulation.Pick"/> or the end of the
    /// encore's time. Then the act goes on.
    /// </summary>
    Encore,

    /// <summary>
    /// An act in which an encore was taken is over and another follows: the world stands while the program offers
    /// the chorus card, until <see cref="Simulation.Pick"/> or the end of the program's time. An act with no
    /// encore has no program.
    /// </summary>
    Program,

    /// <summary>
    /// An act is over, its program's card taken if it had a program, and the next has not begun: the world stands until
    /// <see cref="Simulation.GoOn"/>.
    /// </summary>
    BetweenActs,

    /// <summary>The last act was played to its end: the performance is over, in a standing ovation.</summary>
    Ovation,

    /// <summary>The show has closed, in an act: the performance is over.</summary>
    Closed,
}
