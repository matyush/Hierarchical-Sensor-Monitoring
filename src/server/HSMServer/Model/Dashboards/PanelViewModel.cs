using System;
using System.Linq;
using HSMServer.Core.Model;
using HSMServer.Model.TreeViewModel;
using HSMCommon.Collections;
using HSMServer.Dashboards;
using HSMServer.Extensions;

namespace HSMServer.Model.Dashboards;

public class PanelViewModel
{
    private const string TypeError = "Can't plot using {0} sensor type";
    private const string UnitError = "Can't plot using {0} unit type";

    public CGuidDict<DatasourceViewModel> Sources { get; }


    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid DashboardId { get; set; }
    
    public string Name { get; set; }
    
    public string Description { get; set; }

    public SensorType? SensorType { get; set; }

    public Unit? UnitType { get; set; }


    public PanelViewModel() { }

    public PanelViewModel(Panel panel, Guid dashboardId)
    {
        Name = panel.Name;
        Description = panel.Description;
        Id = panel.Id;
        DashboardId = dashboardId;

        Sources = new CGuidDict<DatasourceViewModel>(panel.Sources.ToDictionary(y => y.Key,
            x => new DatasourceViewModel(x.Value)));
    }

    public bool TryAddSource(SensorNodeViewModel source, out string message)
    {
        if (IsSuits(source.Type, source.SelectedUnit, out message))
        {
            SensorType = source.Type;
            UnitType = source.SelectedUnit;
            message = string.Empty;
            return true;
        }

        return false;
    }

    public void UpdateSources(SensorNodeViewModel source)
    {
        if (IsSuits(source.Type, source.SelectedUnit, out _))
            Sources.TryAdd(source.Id, new DatasourceViewModel(){Id = Guid.NewGuid(), SensorId = source.Id});
    }


    public bool IsSuits(SensorType type, Unit? unit, out string errorMessage) =>
        IsTypeSuits(type, out errorMessage) && IsUnitSuits(unit, out errorMessage);

    public bool IsTypeSuits(SensorType type, out string message)
    {
        var result = IsValidType(type) && (!SensorType.HasValue || SensorType.Value == type);

        message = !result ? string.Format(TypeError, type.ToString()) : string.Empty;

        return result;
    }

    public bool IsUnitSuits(Unit? unit, out string message)
    {
        var result = !UnitType.HasValue || UnitType.Value == unit;
        message = !result ? string.Format(UnitError, unit.ToString()) : string.Empty;

        return result;
    }

    private bool IsValidType(SensorType type) => type != Core.Model.SensorType.File &&
                                                 type != Core.Model.SensorType.Enum &&
                                                 type != Core.Model.SensorType.String &&
                                                 type != Core.Model.SensorType.DoubleBar &&
                                                 type != Core.Model.SensorType.IntegerBar;
}

public class DatasourceViewModel
{
    public Guid Id { get; set; }

    public Guid SensorId { get; set; }

    public SensorType Type { get; set; }

    public string Color { get; set; }

    public string Label { get; set; }


    public DatasourceViewModel() { }

    public DatasourceViewModel(PanelDatasource dataSource)
    {
        Id = dataSource.Id;
        SensorId = dataSource.SensorId;
        Color = dataSource.Color.ToRGB();
        Label = dataSource.Label;
    }
}