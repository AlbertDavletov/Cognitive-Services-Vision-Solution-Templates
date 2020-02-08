import React, { Component } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { RegionState } from '../../models';

const ObjectRegion = (params) => {
    const { activeRegion, selectedRegion, selectedRegionLabelPanel, selectedRegionLabel, circle } = styles;
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
            component = <View style={[positionStyle, selectedRegion]}>
                            <View style={selectedRegionLabelPanel}>
                                <Text numberOfLines={1} style={selectedRegionLabel}>{data.title}</Text>  
                            </View>
                        </View>;
            break;
            
        case RegionState.Active:
        default:
            component = <View style={[positionStyle, activeRegion]}>
                            <View style={circle}/>
                        </View>;
            break;
    }
    return component;
}

const styles = StyleSheet.create({
    activeRegion: {
        // position: 'absolute',
        justifyContent: 'center',
        borderColor: '#248FFF',
        borderWidth: 1,
        backgroundColor: 'rgba(36, 143, 255, 0.6)',
        borderRadius: 1
    },
    selectedRegion: {
        // position: 'absolute',
        borderColor: '#248FFF',
        borderWidth: 2,
        backgroundColor: 'transparent',
        borderRadius: 2
    },
    selectedRegionLabelPanel: {
        top: -32, 
        borderColor: '#248FFF',
        borderWidth: 0,
        margin: -2,
        borderRadius: 1,
        minWidth: 100,
        backgroundColor: '#248FFF'
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
    }
})

export { ObjectRegion };