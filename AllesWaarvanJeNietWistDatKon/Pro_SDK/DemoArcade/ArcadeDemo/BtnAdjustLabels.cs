using ArcGIS.Core.CIM;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Linq;

namespace ArcadeDemo
{
    internal class BtnAdjustLabels : Button
    {
        protected override void OnClick()
        {
            QueuedTask.Run(() =>
            {
                FeatureLayer featureLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().First();
                if (featureLayer.GetDefinition() is not CIMFeatureLayer lyrDefn)
                {
                    return;
                }
                //Get the label classes - we need the first one
                var labelClassesList = lyrDefn.LabelClasses.ToList();
                var labelClass = labelClassesList.FirstOrDefault();

                //set the label class Expression to use the Arcade expression
                labelClass.Expression = "return $feature.Naam + ' ' + $feature.Nummer + TextFormatting.NewLine + 'Waarde: ' + $feature.Nummer * 5;";
                //Set the label definition back to the layer.
                featureLayer.SetDefinition(lyrDefn);
            });
        }
    }
}
