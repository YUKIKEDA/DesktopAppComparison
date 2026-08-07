"""Theme colors and helpers for light/dark modes."""
from __future__ import annotations

from dataclasses import dataclass
from typing import Literal

import wx

ThemeName = Literal["light", "dark"]


@dataclass(frozen=True)
class ThemeColors:
    name: ThemeName
    bg: wx.Colour
    surface: wx.Colour
    surface_alt: wx.Colour
    border: wx.Colour
    text: wx.Colour
    muted_text: wx.Colour
    brand_blue: wx.Colour
    brand_red: wx.Colour
    on_brand: wx.Colour


LIGHT = ThemeColors(
    name="light",
    bg=wx.Colour(243, 244, 246),       # #F3F4F6
    surface=wx.Colour(255, 255, 255),  # #FFFFFF
    surface_alt=wx.Colour(249, 250, 251),  # #F9FAFB
    border=wx.Colour(229, 231, 235),   # #E5E7EB
    text=wx.Colour(17, 24, 39),        # #111827
    muted_text=wx.Colour(75, 85, 99),  # #4B5563
    brand_blue=wx.Colour(37, 99, 235),  # #2563EB
    brand_red=wx.Colour(220, 38, 38),   # #DC2626
    on_brand=wx.Colour(255, 255, 255),
)

DARK = ThemeColors(
    name="dark",
    bg=wx.Colour(17, 24, 39),          # #111827
    surface=wx.Colour(31, 41, 55),     # #1F2937
    surface_alt=wx.Colour(55, 65, 81),  # #374151
    border=wx.Colour(75, 85, 99),      # #4B5563
    text=wx.Colour(249, 250, 251),     # #F9FAFB
    muted_text=wx.Colour(209, 213, 219),  # #D1D5DB
    brand_blue=wx.Colour(37, 99, 235),  # #2563EB
    brand_red=wx.Colour(220, 38, 38),   # #DC2626
    on_brand=wx.Colour(255, 255, 255),
)


def get_theme(name: str) -> ThemeColors:
    return DARK if name == "dark" else LIGHT


def apply_window_colors(window: wx.Window, colors: ThemeColors) -> None:
    """Best-effort recursive colour application."""
    try:
        window.SetBackgroundColour(colors.bg)
        window.SetForegroundColour(colors.text)
    except Exception:
        pass

    for child in window.GetChildren():
        _apply_control_colors(child, colors)


def _apply_control_colors(ctrl: wx.Window, colors: ThemeColors) -> None:
    try:
        if isinstance(ctrl, wx.Panel):
            ctrl.SetBackgroundColour(colors.surface)
            ctrl.SetForegroundColour(colors.text)
        elif isinstance(ctrl, wx.StaticText):
            ctrl.SetForegroundColour(colors.text)
            ctrl.SetBackgroundColour(colors.surface)
        elif isinstance(ctrl, (wx.TextCtrl, wx.ComboBox, wx.Choice)):
            ctrl.SetBackgroundColour(colors.surface_alt)
            ctrl.SetForegroundColour(colors.text)
        elif isinstance(ctrl, wx.ListCtrl):
            ctrl.SetBackgroundColour(colors.surface)
            ctrl.SetForegroundColour(colors.text)
        elif isinstance(ctrl, wx.Button):
            # Leave branded buttons to callers; default outlined look
            if not getattr(ctrl, "_branded", False):
                ctrl.SetBackgroundColour(colors.surface_alt)
                ctrl.SetForegroundColour(colors.text)
        else:
            ctrl.SetBackgroundColour(colors.surface)
            ctrl.SetForegroundColour(colors.text)
    except Exception:
        pass

    for child in ctrl.GetChildren():
        _apply_control_colors(child, colors)


def style_brand_button(button: wx.Button, color: wx.Colour, on_color: wx.Colour) -> None:
    button._branded = True  # type: ignore[attr-defined]
    button.SetBackgroundColour(color)
    button.SetForegroundColour(on_color)
    try:
        # Prefer solid look where supported
        if hasattr(wx, "BU_EXACTFIT"):
            pass
    except Exception:
        pass


def show_with_blend(window: wx.Window, timeout_ms: int = 175) -> bool:
    """Best-effort show animation. Returns True if effect was attempted."""
    try:
        if hasattr(window, "ShowWithEffect"):
            window.ShowWithEffect(wx.SHOW_EFFECT_BLEND, timeout=timeout_ms)
            return True
    except Exception:
        pass
    return False
