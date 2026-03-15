from datetime import timedelta
import logging

from homeassistant.helpers.aiohttp_client import async_get_clientsession
from homeassistant.helpers.update_coordinator import DataUpdateCoordinator, UpdateFailed

from .const import DOMAIN

_LOGGER = logging.getLogger(__name__)
SCAN_INTERVAL = timedelta(seconds=30)


class EnetCoordinator(DataUpdateCoordinator):
    def __init__(self, hass, url):
        super().__init__(hass, _LOGGER, name=DOMAIN, update_interval=SCAN_INTERVAL)
        self.url = url
        self._session = async_get_clientsession(hass)

    async def _async_update_data(self):
        try:
            async with self._session.get(f"{self.url}/things") as resp:
                resp.raise_for_status()
                return await resp.json()
        except Exception as err:
            raise UpdateFailed(f"Error fetching eNet data: {err}") from err

    async def async_post(self, path):
        async with self._session.post(f"{self.url}{path}") as resp:
            resp.raise_for_status()
