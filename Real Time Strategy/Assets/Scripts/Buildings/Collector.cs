﻿public class Collector : BuildingTypeBase
{
    // Start is called before the first frame update
    void Start()
    {
        base.Start();

        canBuild.Add(BuildingType.collector);
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
    }

    protected override void LeftClick()
    {
        base.LeftClick();

    }

}
