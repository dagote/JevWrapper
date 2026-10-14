"""Automatic per-group signal storage for a Jev client."""

from typing import Dict, Optional


class JevContext:
    """Automatic per-group latest-value store, owned by one JevClient.
    
    No config, no history window. Just stores the latest value for each signal
    in each group.
    """

    def __init__(self):
        self._groups: Dict[str, Dict[str, float]] = {}

    def update(self, name: str, group: str, value: float) -> None:
        """Update a signal value in a group.
        
        Args:
            name: Signal name
            group: Group name
            value: Signal value
        """
        if not group:
            return
        
        if group not in self._groups:
            self._groups[group] = {}
        
        self._groups[group][name] = value

    def snapshot(self, group: str) -> Dict[str, float]:
        """Get a copy of all signals in a group.
        
        Args:
            group: Group name
            
        Returns:
            Dictionary of signal name to value, or empty dict if group not found
        """
        if group in self._groups:
            return dict(self._groups[group])
        return {}

    def clear(self, group: Optional[str] = None) -> None:
        """Clear signals.
        
        Args:
            group: If provided, clear only this group. If None, clear all groups.
        """
        if group is None:
            self._groups.clear()
        else:
            self._groups.pop(group, None)
