from homeassistant.components.light import LightEntity, ColorMode, ATTR_BRIGHTNESS
from homeassistant.helpers.update_coordinator import CoordinatorEntity

from .const import DOMAIN


async def async_setup_platform(hass, config, async_add_entities, discovery_info=None):
    coordinator = hass.data[DOMAIN]
    async_add_entities(
        EnetLight(coordinator, thing)
        for thing in coordinator.data
        if thing["type"] == "dimmer"
    )


class EnetLight(CoordinatorEntity, LightEntity):
    _attr_color_mode = ColorMode.BRIGHTNESS
    _attr_supported_color_modes = {ColorMode.BRIGHTNESS}

    def __init__(self, coordinator, thing):
        super().__init__(coordinator)
        self._channel = thing["channel"]
        self._attr_name = thing["name"]
        self._attr_unique_id = f"enet_dimmer_{self._channel}"

    @property
    def _thing(self):
        return next(t for t in self.coordinator.data if t["channel"] == self._channel)

    @property
    def is_on(self):
        state = self._thing.get("state")
        if not state or state.get("value", -1) < 0:
            return None
        return not state.get("isUp", True)

    @property
    def brightness(self):
        state = self._thing.get("state")
        if not state:
            return None
        value = state.get("value")
        if value is None or value < 0:
            return None
        return round(value * 255 / 100)  # eNet 0-100 → HA 0-255

    async def async_turn_on(self, **kwargs):
        if ATTR_BRIGHTNESS in kwargs:
            enet_value = round(kwargs[ATTR_BRIGHTNESS] * 100 / 255)
            await self.coordinator.async_post(f"/things/{self._channel}/brightness/{enet_value}")
        else:
            await self.coordinator.async_post(f"/things/{self._channel}/down")
        await self.coordinator.async_request_refresh()

    async def async_turn_off(self, **kwargs):
        await self.coordinator.async_post(f"/things/{self._channel}/up")
        await self.coordinator.async_request_refresh()
