/* eslint-disable no-unused-vars */
/// <reference types="svelte" />

declare global {
	interface Load {
		name: string
		value: number
	}

	interface Monitors {
		name: string
		resolution: string
		refreshRate: string
	}

	interface Sensor {
		name?: string
		value: number
		min: number
		max: number
	}

	interface Disk {
		name: string
		temperature: number
		usedSpace: number
		size: number
	}

	interface RAMModule {
		bankLabel: string
		capacity: number
		formFactor: number
		manufacturer: string
		maxVoltage: number
		minVoltage: number
		partNumber: string
		serialNumber: string
		speed: number
	}

	interface HardwareInfo {
		cpu: {
			name: string
			temperature: Sensor[]
			lastLoad: number
			power: Sensor[]
		}

		gpu: {
			name: string
			temperature: Sensor[]
			lastLoad: number
			fans: Load[]
			memory: Load[]
			power: Sensor[]
		}

		ram: {
			load: Load[]
			modules: RAMModule[]
		}

		system: {
			os: {
				name: string
				app: string
				webView: string
			}

			storage: {
				disks: Disk[]
			}

			motherboard: {
				name: string
			}

			monitor: {
				monitors: Monitors[]
			}
		}
	}
}

export {}
