import asyncio
import logging

import voluptuous as vol
import homeassistant.helpers.config_validation as cv
from homeassistant.helpers.discovery import async_load_platform

from .const import DOMAIN, DEFAULT_URL
from .coordinator import EnetCoordinator

_LOGGER = logging.getLogger(__name__)

CONFIG_SCHEMA = vol.Schema(
    {DOMAIN: vol.Schema({vol.Optional("url", default=DEFAULT_URL): cv.string})},
    extra=vol.ALLOW_EXTRA,
)

PLATFORMS = ["cover", "switch", "light"]

_RETRY_INTERVAL = 5  # seconds between retries


async def async_setup(hass, config):
    url = config.get(DOMAIN, {}).get("url", DEFAULT_URL)
    coordinator = EnetCoordinator(hass, url)
    hass.data[DOMAIN] = coordinator

    async def _load_platforms_when_ready():
        attempt = 0
        while True:
            attempt += 1
            await coordinator.async_refresh()
            if coordinator.data is not None:
                break
            _LOGGER.warning(
                "eNet: add-on not ready (attempt %d), retrying in %ds",
                attempt, _RETRY_INTERVAL,
            )
            await asyncio.sleep(_RETRY_INTERVAL)

        _LOGGER.info("eNet: add-on ready after %d attempt(s)", attempt)
        for platform in PLATFORMS:
            await async_load_platform(hass, platform, DOMAIN, {}, config)

    hass.async_create_task(_load_platforms_when_ready())
    return True
