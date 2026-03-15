from homeassistant.components.cover import CoverEntity, CoverEntityFeature, CoverDeviceClass
from homeassistant.helpers.update_coordinator import CoordinatorEntity

from .const import DOMAIN


async def async_setup_platform(hass, config, async_add_entities, discovery_info=None):
    coordinator = hass.data[DOMAIN]
    async_add_entities(
        EnetCover(coordinator, thing)
        for thing in coordinator.data
        if thing["type"] == "blind"
    )


class EnetCover(CoordinatorEntity, CoverEntity):
    _attr_device_class = CoverDeviceClass.BLIND

    def __init__(self, coordinator, thing):
        super().__init__(coordinator)
        self._channel = thing["channel"]
        self._attr_name = thing["name"]
        self._attr_unique_id = f"enet_blind_{self._channel}"

    @property
    def _thing(self):
        return next(t for t in self.coordinator.data if t["channel"] == self._channel)

    @property
    def is_closed(self):
        state = self._thing.get("state")
        if not state:
            return None
        return not state.get("isUp", False)

    @property
    def current_cover_position(self):
        state = self._thing.get("state")
        if not state or not state.get("isPositionAware"):
            return None
        value = state.get("value")
        if value is None or value > 100:
            return None
        return 100 - value  # eNet: 0=open, 100=closed → HA: 100=open, 0=closed

    @property
    def supported_features(self):
        features = CoverEntityFeature.OPEN | CoverEntityFeature.CLOSE
        state = self._thing.get("state")
        if state and state.get("isPositionAware"):
            features |= CoverEntityFeature.SET_POSITION
        return features

    async def async_open_cover(self, **kwargs):
        await self.coordinator.async_post(f"/things/{self._channel}/up")
        await self.coordinator.async_request_refresh()

    async def async_close_cover(self, **kwargs):
        await self.coordinator.async_post(f"/things/{self._channel}/down")
        await self.coordinator.async_request_refresh()

    async def async_set_cover_position(self, **kwargs):
        enet_value = 100 - kwargs["position"]  # invert: HA 100=open → eNet 0=open
        await self.coordinator.async_post(f"/things/{self._channel}/position/{enet_value}")
        await self.coordinator.async_request_refresh()
