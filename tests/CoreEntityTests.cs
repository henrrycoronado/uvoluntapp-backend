using System;
using Xunit;
using U_VoluntApp_Core.Src.Domain.Entities;
using CoreProgram = U_VoluntApp_Core.Src.Domain.Entities.Program;
namespace U_VoluntApp_Core.Tests;
public class CoreEntityTests { [Fact] public void ProgramCanActivateAndDeactivate(){var p=new CoreProgram();p.Activate();p.Deactivate();} [Fact] public void ActivityRejectsInvalidCancellation(){Assert.Throws<InvalidOperationException>(()=>new Activity().Cancel(""));} }
