import voluptuous as vol
import homeassistant.helpers.config_validation as cv
from homeassistant.helpers.discovery import async_load_platform

from .const import DOMAIN, DEFAULT_URL
from .coordinator import EnetCoordinator

CONFIG_SCHEMA = vol.Schema(
    {DOMAIN: vol.Schema({vol.Optional("url", default=DEFAULT_URL): cv.string})},
    extra=vol.ALLOW_EXTRA,
)

PLATFORMS = ["cover", "switch"]


async def async_setup(hass, config):
    url = config.get(DOMAIN, {}).get("url", DEFAULT_URL)
    coordinator = EnetCoordinator(hass, url)
    await coordinator.async_refresh()
    hass.data[DOMAIN] = coordinator

    for platform in PLATFORMS:
        hass.async_create_task(
            async_load_platform(hass, platform, DOMAIN, {}, config)
        )

    return True
