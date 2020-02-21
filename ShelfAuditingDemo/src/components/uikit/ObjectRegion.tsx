import React from 'react'
import { View, Text, StyleSheet } from 'react-native'
import { RegionState } from '../../models'
import { Util } from '../../../Util'

interface RegionProps {
    position: any;
    data: any;
}

export const ObjectRegion = (params: RegionProps) => {
    const { 
        activeRegion, 
        selectedRegion, selectedRegionLabelPanel, selectedRegionLabel, 
        circle, 
        disabledRegion, smallCircle 
    } = styles;

    const { position, data } = params;
    const positionStyle = StyleSheet.create({ 
        left: position.left, 
        top: position.top,
        width: position.width,
        height: position.height
    });

    let component;
    switch (data.state) {
        case RegionState.Selected:
            component = <View style={[positionStyle, selectedRegion, { borderColor: data.color }]}>
                            <View style={[selectedRegionLabelPanel, { borderColor: data.color, backgroundColor: data.color }]}>
                                <Text numberOfLines={1} style={selectedRegionLabel}>{data.title}</Text>  
                            </View>
                        </View>;
            break;

        case RegionState.Disabled:
            component = <View style={[positionStyle, disabledRegion, { 
                                      borderColor: data.color,
                                      backgroundColor: Util.SetOpacityToColor(data.color, 0.4) 
                            }]}>
                            <View style={[smallCircle, { backgroundColor: data.color }]}/>
                        </View>;
            break;

        case RegionState.Active:
        default:
            component = <View 
                            style={[positionStyle, activeRegion, { 
                                    borderColor: data.color, 
                                    backgroundColor: Util.SetOpacityToColor(data.color, 0.6) 
                            }]}>
                            <View style={circle}/>
                        </View>;
            break;
    }
    return component;
}

const styles = StyleSheet.create({
    activeRegion: {
        justifyContent: 'center',
        borderWidth: 1,
        borderRadius: 1
    },
    selectedRegion: {
        borderWidth: 2,
        backgroundColor: 'transparent',
        borderRadius: 2
    },
    disabledRegion: {
        justifyContent: 'center',
        borderWidth: 0,
        borderRadius: 1
    },
    selectedRegionLabelPanel: {
        top: -32, 
        borderWidth: 0,
        margin: -2,
        borderRadius: 1,
        minWidth: 100,
    },
    selectedRegionLabel: {
        color: 'white',
        padding: 4
    },
    circle: {
        width: 12,
        height: 12,
        borderRadius: 6,
        backgroundColor: 'rgba(255, 255, 255, 0.8)',
        alignSelf: 'center'
    },
    smallCircle: {
        width: 8,
        height: 8,
        borderRadius: 4,
        alignSelf: 'center'
    }
})
