<template>
  <div class="mapdiv"></div>
</template>

<script>
import WebMap from "@arcgis/core/WebMap";
import MapView from "@arcgis/core/views/MapView";

export default {
  name: 'App',
  async mounted() {
    const webmap = new WebMap({
      portalItem: {
        id: "aa1d3f80270146208328cf66d022e09c",
      },
    });

    const view = new MapView({
      // https://v3.vuejs.org/api/instance-properties.html#el
      container: this.$el,
      map: webmap,
    });

  }
}
</script>

<style>
  @import './main.css';
</style>
