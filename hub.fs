FeatureScript 2491;
import(path : "onshape/std/common.fs", version : "2491.0");

annotation { "Feature Type Name" : "hub", "Feature Type Description" : "" }
export const myFeature = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "part", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
        definition.part is Query;
        annotation { "Name" : "face", "Filter" : EntityType.FACE, "MaxNumberOfPicks" : 1 }
        definition.face is Query;
        annotation { "Name" : "center", "Filter" : EntityType.EDGE, "MaxNumberOfPicks" : 1 }
        definition.center is Query;
        annotation { "Name" : "hub_diam" }
        isLength(definition.hub_diam, LENGTH_BOUNDS);
        annotation { "Name" : "shaft_diam" }
        isLength(definition.shaft_diam, LENGTH_BOUNDS);
        annotation { "Name" : "hub_depth" }
        isLength(definition.hub_depth, LENGTH_BOUNDS);
        annotation { "Name" : "bolt_diam" }
        isLength(definition.bolt_diam, LENGTH_BOUNDS);
        annotation { "Name" : "nut_length" }
        isLength(definition.nut_length, LENGTH_BOUNDS);
        annotation { "Name" : "nut_width" }
        isLength(definition.nut_width, LENGTH_BOUNDS);
        annotation { "Name" : "extrude_depth" }
        isLength(definition.extrude_depth, LENGTH_BOUNDS);
        annotation { "Name" : "slot_closest_edge_dist_from_center" }
        isLength(definition.slot_dist, LENGTH_BOUNDS);
        annotation { "Name" : "nut_wac" }
        isLength(definition.nut_wac, LENGTH_BOUNDS);
        
    }
    {
        // take the center point of face, `center` (not sure how)
        // draw two circles one of `hub_diam` the other of `shaft_diam` both centered on `center`        
        // sketch on top face of that extrusion
        // define a rectangle of `nut_length`*`nut_width` that has its edge closest to the center `slot_dist` away from center
        // extrude cut the rectangle down by `extrude_depth`. this will be the centerpoint height of the set screw bolt / nut
        // take the face created by this cut and closest to the center and draw a circle centered at the bottom edges' midpoint of diamter `bolt_diam`
        // then do a two direction cut where the cut towards the center is `slot_dist` and the cut outwards is `hub_diam` (to be safe)
        // draw a sketch on that same face that was just drawn on. find that same midpoint used for the cylindrical cut
        // and draw a hexagon that has a constraint where the bottom corner and one of the edges of the hexagon are midpointed
        // do an extrude cut one way of this hexagon outwards by `nut_width`
        // mirror the hexagon, 2-way cylinder, and rectangular cuts across to the other side of the hub
        
        var cent = evCurveDefinition(context, { "edge" : definition.center }).coordSystem.origin;
        debug(context, cent);
        println(is2dPoint(cent));
        var p = evPlane(context, {
                "face" : definition.face
            });
        // ce
        // create a sketch on `face`
        var sketch1 = newSketch(context, id + "sketch1", {
                "sketchPlane" : definition.face
            });

        skCircle(sketch1, "circle1", {
                    "center" : vector(0, 0) * millimeter,
                    "radius" : definition.hub_diam / 2
                });

        skCircle(sketch1, "circle2", {
                    "center" : vector(0, 0) * millimeter,
                    "radius" : definition.shaft_diam / 2
                });

        skSolve(sketch1);

        var csys is CoordSystem = planeToCSys(p);
        println("CSYS ORIGIN");
        var o = csys.origin;

        var offset_o = o + (vector(0 * millimeter, -definition.hub_depth, 0 * millimeter));
        println(offset_o);

        var csys2 = coordSystem(offset_o, csys.xAxis, csys.zAxis);
        var p2 = plane(csys2);
        // debug(context, p2);
        // take the space between the two circles and extrude by `hub_depth`
        var q = qSketchRegion(id + "sketch1", false);

        // ev
        opExtrude(context, id + "extrude1", {
                    "entities" : q,
                    "direction" : evOwnerSketchPlane(context, { "entity" : q }).normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : definition.hub_depth
                });
        opBoolean(context, id + "boolean1", {
                    "tools" : qUnion(qCreatedBy(id + "extrude1", EntityType.BODY), definition.part),
                    "operationType" : BooleanOperationType.UNION
                });

        var sketch2 = newSketchOnPlane(context, id + "sketch2", {
                "sketchPlane" : p2
            });
        skCircle(sketch2, "circle1", {
                    "center" : vector(0, 0) * millimeter,
                    "radius" : definition.shaft_diam / 2
                });

        // Create sketch entities here
        skSolve(sketch2);
        var q2 = qSketchRegion(id + "sketch2", false);

        // ev
        opExtrude(context, id + "extrude2", {
                    "entities" : q2,
                    "direction" : -evOwnerSketchPlane(context, { "entity" : q2 }).normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : definition.hub_depth
                });

        opBoolean(context, id + "boolean2", {
                    "tools" : qCreatedBy(id + "extrude2", EntityType.BODY),
                    "targets" : qCreatedBy(id + "extrude1", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
        var sketch3 = newSketchOnPlane(context, id + "sketch3", {
                "sketchPlane" : p2
            });

        skRectangle(sketch3, "rectangle1", {
                    "firstCorner" : vector(definition.slot_dist, definition.nut_length / 2),
                    "secondCorner" : vector(definition.slot_dist + definition.nut_width, -definition.nut_length / 2)
                });

        // Create sketch entities here
        skSolve(sketch3);
        var q3 = qSketchRegion(id + "sketch3");
        // debug(context, regionToExtrude);
        opExtrude(context, id + "extrude3", {
                    "entities" : q3,
                    "direction" : -evOwnerSketchPlane(context, { "entity" : q3 }).normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : definition.extrude_depth
                });

        opBoolean(context, id + "boolean3", {
                    "tools" : qCreatedBy(id + "extrude3", EntityType.BODY),
                    "targets" : qCreatedBy(id + "extrude1", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });

        var foo = qCreatedBy(id + "boolean3", EntityType.FACE);
        println(foo);
        var evs = evaluateQuery(context, foo);
        var p3 = evPlane(context, {
                "face" : evs[0] // dis is sussy baka
            });
        // debug(context, p3);
        var sketch4 = newSketchOnPlane(context, id + "sketch4", {
                "sketchPlane" : p3
            });
        skCircle(sketch4, "circle1", {
                    "center" : vector(definition.extrude_depth / 2, 0 * millimeter),
                    "radius" : definition.bolt_diam / 2
                });

        // Create sketch entities here
        skSolve(sketch4);

        var q4 = qSketchRegion(id + "sketch4");
        // debug(context, regionToExtrude);
        opExtrude(context, id + "extrude4", {
                    "entities" : q4,
                    "direction" : evOwnerSketchPlane(context, { "entity" : q4 }).normal,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : definition.slot_dist,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : definition.hub_diam
                });

        opBoolean(context, id + "boolean4", {
                    "tools" : qCreatedBy(id + "extrude4", EntityType.BODY),
                    "targets" : qCreatedBy(id + "extrude1", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });

        var flats = definition.nut_length;
        var wac = definition.nut_wac;
        var val = ((flats/2)/(wac/2));
        var theta = acos(val);
        var ycoord = sin(theta) * (wac/2); // actually the x LMAO HEHE 
        
        
        var sketch5 = newSketchOnPlane(context, id + "sketch5", {
                "sketchPlane" : p3
            });

        skRegularPolygon(sketch5, "polygon1", {
                "center" : vector(definition.extrude_depth / 2, 0 * millimeter),
                "firstVertex" : vector(definition.extrude_depth / 2 - wac/2, 0*millimeter),
                "sides" : 6
        });

        skSolve(sketch5);

        var q5 = qSketchRegion(id + "sketch5");
        // debug(context, regionToExtrude);
        opExtrude(context, id + "extrude5", {
                    "entities" : q5,
                    "direction" : evOwnerSketchPlane(context, { "entity" : q5 }).normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : definition.nut_width
                });

        opBoolean(context, id + "boolean5", {
                    "tools" : qCreatedBy(id + "extrude5", EntityType.BODY),
                    "targets" : qCreatedBy(id + "extrude1", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });


    // cant find mirror

    });
