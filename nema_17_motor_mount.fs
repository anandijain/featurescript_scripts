FeatureScript 2491;
import(path : "onshape/std/common.fs", version : "2491.0");

annotation { "Feature Type Name" : "nema_17_stepper_mount", "Feature Type Description" : "" }
export const nema_17_mount = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {

        annotation { "Name" : "Part to cut", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
        definition.partToCut is Query;

        annotation { "Name" : "face", "Filter" : EntityType.FACE, "MaxNumberOfPicks" : 1 }
        definition.face is Query;
        annotation { "Name" : "bottom edge", "Filter" : EntityType.EDGE, "MaxNumberOfPicks" : 1 }
        definition.botedge is Query;
        annotation { "Name" : "y", "Filter" : EntityType.EDGE, "MaxNumberOfPicks" : 1 }
        definition.y is Query;

        annotation { "Name" : "origin", "Filter" : EntityType.VERTEX, "MaxNumberOfPicks" : 1 }
        definition.origin is Query;

        annotation { "Name" : "z", "Filter" : EntityType.EDGE, "MaxNumberOfPicks" : 1 }
        definition.z is Query;



        // Define the parameters of the feature type
    }
    {
        var endPoints is Query = qAdjacent(definition.botedge, AdjacencyType.VERTEX, EntityType.VERTEX);
        var startPosition is Vector = evVertexPoint(context, {
                "vertex" : qNthElement(endPoints, 0)
            });
        var endPosition is Vector = evVertexPoint(context, {
                "vertex" : qNthElement(endPoints, 1)
            });
        var xDirection is Vector = normalize(endPosition - startPosition);

        var endPointsz is Query = qAdjacent(definition.z, AdjacencyType.VERTEX, EntityType.VERTEX);
        var startPositionz is Vector = evVertexPoint(context, {
                "vertex" : qNthElement(endPointsz, 0)
            });
        var endPositionz is Vector = evVertexPoint(context, {
                "vertex" : qNthElement(endPointsz, 1)
            });
        var zDirection is Vector = normalize(endPositionz - startPositionz);
        var origin_point = evVertexPoint(context, {
                "vertex" : definition.origin
            });
        // var zDirection2 is Vector = evOwnerSketchPlane(context, {
        //             "entity" : definition.face
        //         }).normal;

        var cSys is CoordSystem = coordSystem(origin_point, xDirection, zDirection);
        debug(context, cSys);
        var sketchPlane is Plane = plane(cSys);
        var sketch1 = newSketchOnPlane(context, id + "sketch1", {
                "sketchPlane" : sketchPlane
            });

        var len = evLength(context, {
                "entities" : definition.botedge
            });
        println(len);
        var x = (endPosition - startPosition) / 2;
        var xcent = len / 2;
        var ycent = -42.32 / 2 * millimeter;
        var cent = vector(xcent, ycent);

        var mount_hole_radius = 4.4 / 2 * millimeter;
        skCircle(sketch1, "circle1", {
                    "center" : cent,
                    "radius" : 23 / 2 * millimeter
                });

        skCircle(sketch1, "circle2", {
                    "center" : cent + vector(15.5, -15.5) * millimeter,
                    "radius" : mount_hole_radius
                });
        skCircle(sketch1, "circle3", {
                    "center" : cent + vector(15.5, 15.5) * millimeter,
                    "radius" : mount_hole_radius
                });

        skCircle(sketch1, "circle4", {
                    "center" : cent + vector(-15.5, 15.5) * millimeter,
                    "radius" : mount_hole_radius
                });
        skCircle(sketch1, "circle5", {
                    "center" : cent + vector(-15.5, -15.5) * millimeter,
                    "radius" : mount_hole_radius
                });
        skSolve(sketch1);

        var regionToExtrude = qSketchRegion(id + "sketch1");
        debug(context, regionToExtrude);

        opExtrude(context, id + "extrude1", {
                    "entities" : regionToExtrude,
                    "direction" : zDirection,
                    "endBound" : BoundingType.THROUGH_ALL,
                    "startBound" : BoundingType.THROUGH_ALL
                });
        opBoolean(context, id + "boolean1", {
                    "tools" : qCreatedBy(id + "extrude1", EntityType.BODY),
                    "targets" : definition.partToCut,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
        // Define the function's action
        // makes a sketch on `face`.
        //  NOTCH : circle of `stepper_notch_hole` diam (23 mm)
        // centered to midpoint of bottom edge (v/h constraint)
        // circle is `stepper_width/2` high
        // a circle `stepper_shaft_center_vertical_dist_to_mounting_hole` vert away and horiz away
        // circular pattern that around the center of NOTCH with 4x.
        // then do an opExtrude remove around the body associated with `face`


    });
