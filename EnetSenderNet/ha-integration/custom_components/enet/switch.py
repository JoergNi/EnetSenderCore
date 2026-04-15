from homeassistant.components.switch import SwitchEntity
from homeassistant.helpers.update_coordinator import CoordinatorEntity

from .const import DOMAIN


async def async_setup_platform(hass, config, async_add_entities, discovery_info=None):
    coordinator = hass.data[DOMAIN]
    async_add_entities(
        EnetSwitch(coordinator, thing)
        for thing in coordinator.data
        if thing["type"] == "switch"
    )


class EnetSwitch(CoordinatorEntity, SwitchEntity):
    def __init__(self, coordinator, thing):
        super().__init__(coordinator)
        self._channel = thing["channel"]
        self._attr_name = thing["name"]
        self._attr_unique_id = f"enet_switch_{self._channel}"

    @property
    def _thing(self):
        return next(t for t in self.coordinator.data if t["channel"] == self._channel)

    @property
    def is_on(self):
        state = self._thing.get("state")
        if not state:
            return None
        return not state.get("isUp", True)  # isUp=False means switch is ON (down)

    async def async_turn_on(self, **kwargs):
        await self.coordinator.async_post(f"/things/{self._channel}/down")
        await self.coordinator.async_request_refresh()

    async def async_turn_off(self, **kwargs):
        await self.coordinator.async_post(f"/things/{self._channel}/up")
        await self.coordinator.async_request_refresh()
